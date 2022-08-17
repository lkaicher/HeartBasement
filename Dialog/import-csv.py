import csv

input_file = csv.DictReader(open("CHFDialog.csv"))

for row in input_file:
    if row['Character']: 
        
        print ('tts --text "%s" --out_path Voice/%s%s.wav'%(row['English'],row['Character'],row['ID']))


