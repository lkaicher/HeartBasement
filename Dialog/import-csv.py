import csv
import os
import sys
os.chdir(os.path.dirname(sys.argv[0]))
charMap={"Narr":"Allison","Dave":"Tom","Tony":"Alex","Neighbor2":"Tom",'HardwareClerk':"Oliver"}
phaseMap={"English":"","PhaseOne":"P1/", "PhaseTwo":"P2/"}
input_file = csv.DictReader(open("CHFDialog.csv"))
for row in input_file:
    for phaseIndex, phaseName in enumerate(phaseMap): 

        # print ('say -v %s "%s" -o %s/%s%s.aiff'%(sys.argv[1],charMap[row['Character']], speechText,row['Character'],row['ID']))
        if row[phaseName]:
        
            print (f'say -v {charMap[row["Character"]]} "{row[phaseName]}" -o {sys.argv[1]}{phaseMap[phaseName]}{row["Character"]}{row["ID"]}.aiff')

 #print (f"say -v {charMap[row['Tony']]} 'Again?! Alright, I'll be right over' -o {sys.argv[1]}{'Narr'}{'17'}.aiff")
