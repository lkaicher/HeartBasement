import csv
import os
import sys
os.chdir(os.path.dirname(sys.argv[0]))
charMap={"Narr":"Allison","Dave":"Tom","Tony":"Bruce","Neighbor2":"Tom",'HardwareClerk':"Oliver"}
input_file = csv.DictReader(open("CHFDialog.csv"))

for row in input_file:
    if row['Character']: 
  
        if row['PhaseOne']:
            speachText=row['PhaseOne']         
        else:
            speachText=row['English']
        print ('say -v %s "%s" -o Voice/%s%s.aiff'%(charMap[row['Character']], speachText,row['Character'],row['ID']))
