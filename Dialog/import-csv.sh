VOICEPATH="../Assets/Audio/Resources/Voice/"
WILDCARD=${VOICEPATH}*.aiff
python3 ./import-csv.py $VOICEPATH | sh
for i in $WILDCARD 
do ffmpeg -y -i $i ${i/aiff/m4a} && rm $i 
done