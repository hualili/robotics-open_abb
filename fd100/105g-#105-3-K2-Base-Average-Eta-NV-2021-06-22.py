'''-------------------------------------------------------------------------------------------------------
* Company Name : CTI One Corp                                                                            *
* Program name : 105-3-K2-Base-Average-Eta-NV-2021-06-22.py                                              *
* Status       : Test                                                                                    *
* Coded By     : NV                                                                                      *
* Date         : 2021-06-22                                                                              *                                                                              *
* Version      : v1.0.0                                                                                  *
* Copyright    : Copyright (c) 2021 CTI One Corporation                                                  *
* Purpose      : To Compare Performance of BaseLine Algorithm and K2 Algorithm.                           *
*              : v1.0.0 2021-06-22 NV Created                                                            *
-------------------------------------------------------------------------------------------------------'''
#!/usr/bin/env python
# coding: utf-8

# In[1]:


import matplotlib.pyplot as plt
import pandas as pd


# In[2]:


#Creating a two Lists of 10 DataFrame Objects and a List of 10 Paths each for BaseLine and K2 Algorithms

baseLineList = [pd.DataFrame()] * 10    
baseLinePathList = [""] * 10            

k2List = [pd.DataFrame()] * 10
k2PathList = [""]  * 10


# In[9]:


#Generating Paths for BaseLine and k2 Algorithms
for index in range(0,9):
    baseLinePathList[index] = "trainingData/base/distance-base-0"+ str(index + 1) +"-2021-6-15.csv"
    k2PathList[index] = "trainingData/distance/k2-1p5/distance-k2-1p5-0"+ str(index + 1) +"-2021-6-14.csv"

baseLinePathList[9] = "trainingData/base/distance-base-10-2021-6-15.csv"
k2PathList[9] = "trainingData/distance/k2-1p5/distance-k2-1p5-10-2021-6-14.csv"


# In[10]:


def generateDataFrames():
    for i in range(0,10):
        baseLineList[i] = pd.read_csv(baseLinePathList[i],usecols = ["reward"])
        k2List[i] = pd.read_csv(k2PathList[i],usecols = ["reward"])
        baseLineList[i] = baseLineList[i].cumsum()
        k2List[i] = k2List[i].cumsum()

generateDataFrames()


# In[11]:


#Dropping Rows For BaseLine

recordCounts = [0] * 10
for i in range(0,10):
    recordCounts[i] = len(baseLineList[i].index)
baseLineMaxRange = min(recordCounts)
for i in range(10):
    if (len(baseLineList[i].index)) > (baseLineMaxRange):
        baseLineList[i] = baseLineList[i].drop(range((baseLineMaxRange),(len(baseLineList[i].index)))) #As index starts from 0 and whatever is within range(x,y) is actually from 0 to y-1

#Dropping Rows For K2

k2RecordCounts = [0] * 10
for i in range(0,10):
    k2RecordCounts[i] = len(k2List[i].index)
    
k2MaxRange = min(k2RecordCounts)

for i in range(10):
    if (len(k2List[i].index)) > (k2MaxRange):
        k2List[i] = k2List[i].drop(range((k2MaxRange),(len(k2List[i].index)))) #As index starts from 0 and whatever is within range(x,y) is actually from 0 to y-1   


# In[12]:


# Merges all dataFrames and for each unique value of index from 10 csv files, reward average is generated.
baseLine_df_avg = pd.concat(baseLineList)
baseLine_df_avg = baseLine_df_avg.groupby(baseLine_df_avg.index).mean()

k2_df_avg = pd.concat(k2List)
k2_df_avg = k2_df_avg.groupby(k2_df_avg.index).mean()


# In[13]:


baseLine_df_avg.to_csv('baseLineAverage.csv')
k2_df_avg.to_csv('k2Average.csv')


# In[14]:


baseLineFirstNegIndex = baseLine_df_avg[baseLine_df_avg['reward'] < 0].index[0]
print(f"BaseLine First Negative Index: {baseLineFirstNegIndex}")
baseLineCrossOverPoint = baseLine_df_avg[(baseLine_df_avg['reward'] >= 0) & (baseLine_df_avg.index > baseLineFirstNegIndex)].index[0]
print(f"BaseLine CrossOver Point: {baseLineCrossOverPoint}")

k2FirstNegIndex = k2_df_avg[k2_df_avg['reward'] < 0].index[0]
print(f"K2 First Negative Index: {k2FirstNegIndex}")
k2CrossOverPoint = k2_df_avg[(k2_df_avg['reward'] >= 0) & (k2_df_avg.index > k2FirstNegIndex)].index[0]
print(f"k2 CrossOver Point: {k2CrossOverPoint}")

print(f"Total Steps of BaseLine: {len(baseLine_df_avg.index)}")
print(f"Total Steps of K2: {len(k2_df_avg.index)}")


# In[15]:


# This section is according to the Notes from Professor which computes Eta negative and Eta positive
baseLine_neg_index = baseLine_df_avg[baseLine_df_avg < 0].dropna().sum().round(1)
baseLine_pos_index = baseLine_df_avg[baseLine_df_avg > 0].dropna().sum().round(1)

print("BaseLine Negative Index Summation:", baseLine_neg_index.values)
print("BaseLine Positive Index Summation:", baseLine_pos_index.values)

k2_neg_index = k2_df_avg[k2_df_avg < 0].dropna().sum().round(1)
k2_pos_index = k2_df_avg[k2_df_avg > 0].dropna().sum().round(1)

print("K2 Negative Index Summation:", k2_neg_index.values)
print("K2 Positive Index Summation:", k2_pos_index.values)

negativeImprovement = k2_neg_index/baseLine_neg_index
positiveImprovement = k2_pos_index/baseLine_pos_index

print("ETA Negative:", negativeImprovement.values)
print("ETA Positive:", positiveImprovement.values)


# In[16]:


#Generating DataFrames for Graphs
baseLine_df = pd.DataFrame({'y1':baseLineList[0]['reward'],'y2':baseLineList[1]['reward'],'y3':baseLineList[2]['reward'], 'y4':baseLineList[3]['reward'], 'y5':baseLineList[4]['reward'],'y6':baseLineList[5]['reward'],'y7':baseLineList[6]['reward'],'y8':baseLineList[7]['reward'],'y9':baseLineList[8]['reward'],'y10':baseLineList[9]['reward'],'y11':baseLine_df_avg['reward']})
k2_df = pd.DataFrame({'y1':k2List[0]['reward'],'y2':k2List[1]['reward'],'y3':k2List[2]['reward'], 'y4':k2List[3]['reward'], 'y5':k2List[4]['reward'],'y6':k2List[5]['reward'],'y7':k2List[6]['reward'],'y8':k2List[7]['reward'],'y9':k2List[8]['reward'],'y10':k2List[9]['reward'],'y11':k2_df_avg['reward']})
df_baseLine_k2 = pd.DataFrame({'BaseLine':baseLine_df_avg['reward'],'K2':k2_df_avg['reward']})


# In[17]:


#Dropping rows for missing Values after merger of BaseLine and k2 DataFrame Objects
baseLine_k2_maxRange = min(baseLineMaxRange,k2MaxRange)
df_baseLine_k2 = df_baseLine_k2.drop(range(baseLine_k2_maxRange, len(df_baseLine_k2.index)))


# In[18]:


#Scaling the graph
fig, axs = plt.subplots(3, figsize = (15,20), squeeze = True)

#Plotting the reward values on Y axis for each csv files
axs[0].set_title('BaseLine Algorithm:')
axs[0].set_xlabel("Steps")
axs[0].set_ylabel("Reward")
axs[0].plot('y1',data = baseLine_df, linewidth=1)
axs[0].plot('y2',data = baseLine_df, linewidth=1)
axs[0].plot('y3',data = baseLine_df, linewidth=1)
axs[0].plot('y4',data = baseLine_df, linewidth=1)
axs[0].plot('y5',data = baseLine_df, linewidth=1)
axs[0].plot('y6',data = baseLine_df, linewidth=1)
axs[0].plot('y7',data = baseLine_df, linewidth=1)
axs[0].plot('y8',data = baseLine_df, linewidth=1)
axs[0].plot('y9',data = baseLine_df, linewidth=1)
axs[0].plot('y10',data = baseLine_df,linewidth=1)

#Plotting the Average value of reward from all 10 csv files on Y Axis
axs[0].plot('y11', data = baseLine_df, color = "black", label = "Average", linestyle = 'solid', linewidth = '2')
axs[0].legend()

# Graph for K2 Algorithm
axs[1].set_title('K2 Algorithm:')
axs[1].set_xlabel("Steps")
axs[1].set_ylabel("Reward")
axs[1].plot('y1',data = k2_df, linewidth=1)
axs[1].plot('y2',data = k2_df, linewidth=1)
axs[1].plot('y3',data = k2_df, linewidth=1)
axs[1].plot('y4',data = k2_df, linewidth=1)
axs[1].plot('y5',data = k2_df, linewidth=1)
axs[1].plot('y6',data = k2_df, linewidth=1)
axs[1].plot('y7',data = k2_df, linewidth=1)
axs[1].plot('y8',data = k2_df, linewidth=1)
axs[1].plot('y9',data = k2_df, linewidth=1)
axs[1].plot('y10',data = k2_df,linewidth=1)

#Plotting the Average value of reward from all 10 csv files on Y Axis
axs[1].plot('y11', data = k2_df, color = "black", label = "Average", linestyle = 'solid', linewidth = '2')
axs[1].legend()

#Graph for Comparison between BaseLine and K2 Algorithm
axs[2].plot('BaseLine',label = "BaseLine",data = df_baseLine_k2, linewidth = 1, color = 'black')
axs[2].plot('K2',data = df_baseLine_k2, label = "K2", linewidth = 1, color = 'red')
axs[2].plot(baseLineCrossOverPoint,baseLine_df_avg.iloc[baseLineCrossOverPoint]['reward'], marker = 'o')
axs[2].annotate(f"baseLine Cross Over Point at ({baseLineCrossOverPoint}, {baseLine_df_avg.iloc[baseLineCrossOverPoint]['reward'].round(3)})", xy = (baseLineCrossOverPoint,baseLine_df_avg.iloc[baseLineCrossOverPoint]['reward']),xytext =(baseLineCrossOverPoint + 700, -700))
axs[2].annotate(f"k2 Cross Over Point at ({k2CrossOverPoint}, {k2_df_avg.iloc[k2CrossOverPoint]['reward'].round(3)})", xy = (k2CrossOverPoint,k2_df_avg.iloc[k2CrossOverPoint]['reward']),xytext =(k2CrossOverPoint - 8000, 1000))
axs[2].plot(k2CrossOverPoint,k2_df_avg.iloc[k2CrossOverPoint]['reward'], marker = 'o')
axs[2].set_xlabel('Steps')
axs[2].set_ylabel('Reward')
axs[2].legend()
plt.show()