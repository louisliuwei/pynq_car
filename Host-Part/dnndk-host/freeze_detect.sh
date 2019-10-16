### 
# @Author: Sauron Wu
 # @GitHub: wutianze
 # @Email: 1369130123qq@gmail.com
 # @Date: 2019-10-15 15:10:09
 # @LastEditors: Sauron Wu
 # @LastEditTime: 2019-10-15 15:26:39
 # @Description: 
 ###
# please pay attention that the .h5 file must be generated by keras rather than tensorflow.keras
python keras_to_tensorflow.py --input_model="../model/model.h5" --output_model="./model.pb"

# decent_q will tell you the CONV_INPUT(input_nodes) name and CONV_OUTPUT(output_nodes) name, use them to replace the ones in process_img.py and quant.sh
decent_q inspect --input_frozen_graph=./model.pb