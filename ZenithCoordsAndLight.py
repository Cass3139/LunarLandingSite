"""
Last update : 11 nov. 2026
By : Cassandre Poirier

Objective : Finding the sub-solar coordinates of the moon at a precise time using the skyfield python module.

References :
    https://pypi.org/project/skyfield/
    https://rhodesmill.org/skyfield/planetary.html#computing-lunar-libration

"""

from skyfield.api import PlanetaryConstants, load
import numpy


STEF_BOLT_CST = 5.670367E-8       #Stefan-Boltzmann constant, W/(m^2k^4)
SUN_SURFACE_TEMP = 5800           #K
SUN_RADIUS = 6.96E8               #m
SOLAR_FLUX = 4*numpy.pi*SUN_RADIUS**2*STEF_BOLT_CST*SUN_SURFACE_TEMP**4

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

    max_irradiance = calculate_irradiance(MAX_SIN, sub_sol_coordinates_solstice[2])





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
    lat = numpy.radians(lat)
    lon = numpy.radians(lon)
    latz = numpy.radians(latz)
    lonz = numpy.radians(lonz)

    sin_elevation = numpy.sin(lat)*numpy.sin(latz) + numpy.cos(lat)*numpy.cos(latz)*numpy.cos(lon-lonz)

    elevation_deg = numpy.degrees(numpy.asin(sin_elevation))

    return elevation_deg, sin_elevation



def calculate_irradiance(sin_elevation, moon_sun_distance_m):
    print(sin_elevation)
    if sin_elevation <= 0:  #If the Sun is at the horizon or below
        return 0

    return (SOLAR_FLUX*sin_elevation)/(4*numpy.pi*moon_sun_distance_m**2)  #Irradiance in W/m^2





if __name__=="__main__": 
    main()




