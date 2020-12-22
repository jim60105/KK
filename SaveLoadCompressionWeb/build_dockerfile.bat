@ECHO OFF
cd ..
docker build -t test -f .\SaveLoadCompressionWeb\Dockerfile .
PAUSE