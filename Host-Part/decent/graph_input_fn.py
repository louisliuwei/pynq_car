import cv2
import os
import numpy as np

calib_batch_size = 50
def calib_input(iter):
  images = []
  path = "/home/sauron/pynq_car/sdsandbox/sdsim/lsr-pid2/"
  files = os.listdir(path)
  for index in range(0, calib_batch_size):
    if files[iter*calib_batch_size+index] == "train.csv":
        continue
    image = cv2.imread(path + files[iter*calib_batch_size + index], cv2.IMREAD_UNCHANGED)

    # scale the pixel values to range 0 to 1.0
    #image = image/255.0
    #print(image.shape)
    image = image.reshape((image.shape[0], image.shape[1], 3))
    #print(image.shape)
    image = image[40:,:]
    image = image/255.0 - 0.5
    images.append(image)
  return {"conv2d_1_input": images}


