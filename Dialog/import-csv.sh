VOICEPATH="../Assets/Audio/Resources/Voice/"
WILDCARD=${VOICEPATH}*.aiff
python3 ./import-csv2.py $VOICEPATH | sh
for i in $WILDCARD 
do ffmpeg -y -i $i ${i/aiff/wav}
done