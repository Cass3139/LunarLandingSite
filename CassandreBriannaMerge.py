

import rasterio
import numpy as np
from skyfield.api import PlanetaryConstants, load

SITE_NUMBER = "Site01_final_adj_5mpp_slp.tif"

#CONSTANTS
STEF_BOLT_CST = 5.670367E-8       #Stefan-Boltzmann constant, W/(m^2k^4)
SUN_SURFACE_TEMP = 5800           #K
SUN_RADIUS = 6.96E8               #m
SOLAR_FLUX = 4*np.pi*SUN_RADIUS**2*STEF_BOLT_CST*SUN_SURFACE_TEMP**4

light_weight = 0.6
topology_weight = 0.4
MAXIMUM_AVERAGE_SLOPE = 15

def main():
    MAX_SIN = 1

    #Dummy values before implementing an input
    lat = 90
    lon = 90
    year = 2026
    month = 12
    day = 21
    hour = 21
    minute = 50
    second = 0


    timescale = create_timescale(year, month, day, hour, minute, second)
    moon_frame = build_moon_frame()

    sub_sol_coordinates = find_subsolar_coord(moon_frame, timescale) #Subsolar coordinates
    latz_deg, longz_deg, moon_sun_distance_m = sub_sol_coordinates
    
    sub_sol_coordinates_solstice = find_subsolar_coord(moon_frame, create_timescale(2026, 11, 21, 21, 50, 0)) #Solstice 2026
    
    sun_elevation, sin_sun_elevation = calculate_elevation_angle(lat, lon, latz_deg, longz_deg)
    irradiance = calculate_irradiance(sin_sun_elevation, moon_sun_distance_m)

    maximum_irradiance = calculate_irradiance(MAX_SIN, sub_sol_coordinates_solstice[2])


    print("Topology score: " , topology_score())
    print("Irradiance score: " , light_score(maximum_irradiance, sin_sun_elevation, moon_sun_distance_m))


def create_timescale(year=2026, month=1, day=1, hour=0, minute=0, second=0):     #Create timescale (with UTC time)
    timescale = load.timescale()
    return timescale.utc(year, month, day, hour, minute, second)



def build_moon_frame():

    PLANETARY_CONSTANTS = PlanetaryConstants()
    PLANETARY_CONSTANTS.read_text(load('moon_080317.tf'))                   #Lunar frame specifications
    PLANETARY_CONSTANTS.read_text(load('pck00008.tpc'))                     #Orientation constants and radii of celestial bodies
    PLANETARY_CONSTANTS.read_binary(load('moon_pa_de421_1900-2050.bpc'))    #Same information, but binary
    
    return PLANETARY_CONSTANTS.build_frame_named('MOON_PA_DE421')           #Build a frame for the moon using these constants



def find_subsolar_coord(moon_frame, timescale):
    #Load ephemerides
    eph = load('de421.bsp') 
    sun_data = eph['sun']
    moon_data = eph['moon']


    #Sub-solar coordinates using the geometric difference between the Sun and the Moon
    apparent_sun = (sun_data - moon_data).at(timescale)
    latitude, longitude, distance_au = apparent_sun.frame_latlon(moon_frame)


    #Conversions to degrees and meters
    latitude_deg = latitude.degrees
    longitude_deg = (longitude.degrees + 180.0) % 360.0 - 180.0
    distance_m = distance_au.m


    sub_sol_coordinates = (latitude_deg, longitude_deg, distance_m)

    return sub_sol_coordinates



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


#Return how ideal the luminance is at a certain place and time on a scale of 1 to 10
def light_score(maximum_irradiance, sin_elevation, moon_sun_distance_m):
    light_watt = calculate_irradiance(sin_elevation, moon_sun_distance_m)
    if 0 <= light_watt and light_watt <= maximum_irradiance/10:
        return 1
    for i in range(1,10):
        if i*maximum_irradiance/10 < light_watt and light_watt<= (i+1)*maximum_irradiance/10:
            return i+1

#Returns the average slope at a certain area according to NASA datasets
def get_slope():
    with rasterio.open(SITE_NUMBER) as dataset:
        mask = dataset.read(1)
        slope = np.sum(np.abs(np.array(mask))) / (dataset.width * dataset.height)
    return slope

#Returns how ideal the topology is for landing on a scale of 1 to 10
def topology_score():
    average_slope = get_slope()
    return int(np.round(MAXIMUM_AVERAGE_SLOPE-average_slope))
#We choose to have 15 degrees be the maximum slope for a good topology, and we subtract the actual slope to get the score. Therefore, a slope of 0 degrees has a perfect score, and any slope greater than 15 degrees has a score of 0.

#Returns the total score. It is weighted bassed on how important every criteria is. 
"""def calculate_scores(): 
    #If there isnt a direct line of communication with the earth, the score automatically drops to 0.
    if calcul_comm(latz,lat,lon) == False: #AJOUTER FONCTION DE CASSANDRE
        total_score = 0
        return total_score
    else:
        total_score = light_weight*light_score() + topology_weight*topology_score()
        return total_score"""

if __name__=="__main__": 
    main()