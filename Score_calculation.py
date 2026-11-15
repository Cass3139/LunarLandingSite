#Score calculation

import rasterio
import rasterio.features
import numpy as np


SITE_NUMBER = "Site01_final_adj_5mpp_slp.tif"
maximum_irradiance = 136 #W/m^2 
light_weight = 0.6
topology_weight = 0.4

#Return how ideal the luminance is at a certain place and time on a scale of 1 to 10
def light_score():
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
        print(dataset.shape)
        print(mask)
        slope = np.sum(np.abs(np.array(mask))) / (dataset.width * dataset.height)
    return slope

#Returns how ideal the topology is for landing on a scale of 1 to 10
def topology_score():
    slope = get_slope()
    return np.round(15-slope,2)
#We choose to have 15 degrees be the maximum slope for a good topology, and we subtract the actual slope to get the score. Therefore, a slope of 0 degrees has a perfect score, and any slope greater than 15 degrees has a score of 0.

#Returns the total score. It is weighted bassed on how important every criteria is. 
def calculate_scores(): 
    #If there isnt a direct line of communication with the earth, the score automatically drops to 0.
    if calcul_comm(latz,lat,lon) == False: #AJOUTER FONCTION DE CASSANDRE
        total_score = 0
        return total_score
    else:
        total_score = light_weight*light_score() + topology_weight*topology_score()
        return total_score
