# Lunar Landing Site
NASA space apps challenge 2026

## Required python modules to run main.py
skyfield\
rasterio\
numpy\
csv

## Notice
The current program only includes informations for 3 landing sites : 01-Connecting Ridge, 04 - Shackleton rim and Haworth.

## Variables used to evaluate the sites
Light score : score based on the irradiance of the Sun at precise coordinates on the Moon to evaluate the solar power potential.\
Topology score : score based on the average slope around the landing site to account for the site itself and surrounding mountains and/or craters.\
Communication status (bool) : whether or not the Earth is in the sky at the site at a certain time, allowing for a direct line of communication.
