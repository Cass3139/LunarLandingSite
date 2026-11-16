"""
References :
    https://pypi.org/project/skyfield/
    https://rhodesmill.org/skyfield/planetary.html#computing-lunar-libration
    https://pgda.gsfc.nasa.gov/data/LOLA_5mpp/
"""

from skyfield.api import PlanetaryConstants, load
import numpy as np
import rasterio
import csv
import os
import sys

DIR_PATH = sys.argv[1]
PATH_TO_CSV = sys.argv[2]


STEF_BOLT_CST = 5.670367E-8       #Stefan-Boltzmann constant, W/(m^2k^4)
SUN_SURFACE_TEMP = 5800           #K
SUN_RADIUS = 6.96E8               #m
SOLAR_FLUX = 4*np.pi*SUN_RADIUS**2*STEF_BOLT_CST*SUN_SURFACE_TEMP**4
MAXIMUM_AVERAGE_SLOPE = 15

"""
#Add a user's slider for weight input?
light_weight = 0.6
topology_weight = 0.4
"""

#Loading all the ephemerid of all the bodies 
eph = load('de421.bsp') 
sun_data = eph['sun']
moon_data = eph['moon']
earth_data = eph['earth']


def main():   
    with open(PATH_TO_CSV, "r", encoding="UTF-8", newline="") as csvfile:
        data = csv.DictReader(csvfile)    
        for row in data:
            site_file = row["site_file"]
            lat = float(row["lat"])
            lon = float(row["lon"])
            year = int(row["year"])
            month = int(row["month"])
            day = int(row["day"])
            hour = int(row["hour"])


    timescale = create_timescale(year, month, day, hour)
    
    moon_frame = build_moon_frame()

    sun_elevation, light_score, irradiance, max_irradiance = find_solar_info(moon_frame, timescale, lat, lon)
    comms_status = check_comms(moon_frame, timescale, lat, lon)

    topology_score = find_topology_score(site_file)


    with open(PATH_TO_CSV, "w", encoding="UTF-8", newline="") as csvfile:
        writer = csv.writer(csvfile)
        writer.writerow(["site_file", "lat", "lon", "year" , "month", "day", "hour", "light_score", "topology_score", "comms_status"])
        writer.writerows([[site_file, lat, lon, year, month, day, hour, light_score, topology_score, comms_status]])



def create_timescale(year=2026, month=1, day=1, hour=0):     #Create timescale (with UTC time)
    timescale = load.timescale()
    return timescale.utc(int(year), int(month), int(day), int(hour))



def build_moon_frame():
    PLANETARY_CONSTANTS = PlanetaryConstants()
    PLANETARY_CONSTANTS.read_text(load('moon_080317.tf'))                   #Lunar frame specifications
    PLANETARY_CONSTANTS.read_text(load('pck00008.tpc'))                     #Orientation constants and radii of celestial bodies
    PLANETARY_CONSTANTS.read_binary(load('moon_pa_de421_1900-2050.bpc'))    #Same information, but binary
    
    return PLANETARY_CONSTANTS.build_frame_named('MOON_PA_DE421')           #Build a frame for the moon using these constants



def find_libration_coord(moon_frame, target_body_data, timescale):

    #Sub-solar coordinates using the geometric difference between the Sun and the Moon
    apparent_body = (target_body_data - moon_data).at(timescale)
    latitude, longitude, distance_au = apparent_body.frame_latlon(moon_frame)

    #Conversions to degrees and meters
    latitude_deg = latitude.degrees
    longitude_deg = (longitude.degrees + 180.0) % 360.0 - 180.0
    distance_m = distance_au.m

    return (latitude_deg, longitude_deg, distance_m)



def calculate_elevation_angle(lat, lon, latz, lonz):
    #Conversion to rads to use math's trig fcts
    lat = np.radians(lat)
    lon = np.radians(lon)
    latz = np.radians(latz)
    lonz = np.radians(lonz)

    sin_elevation = np.sin(lat)*np.sin(latz) + np.cos(lat)*np.cos(latz)*np.cos(lon-lonz)

    elevation_deg = np.degrees(np.asin(sin_elevation))

    return elevation_deg, sin_elevation


def calculate_irradiance(sin_elevation, moon_sun_distance_m):
    if sin_elevation <= 0:  #If the Sun is at the horizon or below
        return 0
    return (SOLAR_FLUX*sin_elevation)/(4*np.pi*moon_sun_distance_m**2)  #Irradiance in W/m^2


def find_solar_info(moon_frame, timescale, lat, lon):
    MAX_SIN = 1

    sub_sol_coordinates = find_libration_coord(moon_frame, sun_data, timescale) #Subsolar coordinates
    latz_deg, longz_deg, moon_sun_distance_m = sub_sol_coordinates
    
    sub_sol_coordinates_solstice = find_libration_coord(moon_frame, sun_data, create_timescale(2026, 11, 21, 21)) #Solstice 2026
    
    sun_elevation, sin_sun_elevation = calculate_elevation_angle(lat, lon, latz_deg, longz_deg)
    irradiance = calculate_irradiance(sin_sun_elevation, moon_sun_distance_m)
    max_irradiance = calculate_irradiance(MAX_SIN, sub_sol_coordinates_solstice[2])

    light_score = find_light_score(max_irradiance, sin_sun_elevation, moon_sun_distance_m)

    return sun_elevation, light_score, irradiance, max_irradiance


def check_comms(moon_frame, timescale, lat, lon):

    earth_libration_coordinates = find_libration_coord(moon_frame, earth_data, timescale) #Subsolar coordinates
    latz_deg, longz_deg, moon_earth_distance_m = earth_libration_coordinates
        
    earth_elevation, sin_earth_elevation = calculate_elevation_angle(lat, lon, latz_deg, longz_deg)

    if earth_elevation <= 0:
        return False
    return True 


#Return how ideal the luminance is at a certain place and time on a scale of 1 to 10
def find_light_score(maximum_irradiance, sin_elevation, moon_sun_distance_m):
    light_watt = calculate_irradiance(sin_elevation, moon_sun_distance_m)
    if 0 <= light_watt and light_watt <= maximum_irradiance/10:
        return 1
    for i in range(1,10):
        if i*maximum_irradiance/10 < light_watt and light_watt<= (i+1)*maximum_irradiance/10:
            return i+1


#Returns the average slope at a certain area according to NASA datasets
def get_slope(site_file):
    path = os.path.join(DIR_PATH, site_file)
    with rasterio.open(path) as dataset:
        mask = dataset.read(1)
        print(dataset.transform)
        slope = np.sum(np.abs(np.array(mask))) / (dataset.width * dataset.height)
    return slope


#Returns how ideal the topology is for landing on a scale of 1 to 10
def find_topology_score(site_file):
    average_slope = get_slope(site_file)
    if average_slope > MAXIMUM_AVERAGE_SLOPE:
        return 0
    return int(np.round(MAXIMUM_AVERAGE_SLOPE-average_slope))
#We chose to have 15 degrees be the maximum slope for a good topology, and we subtract the actual slope to get the score. Therefore, a slope of 0 degrees has a perfect score, and any slope greater than 15 degrees has a score of 0.




if __name__=="__main__": 
    main()
