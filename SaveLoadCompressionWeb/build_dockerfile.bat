@ECHO OFF
cd ..
docker build -t web -f .\SaveLoadCompressionWeb\Dockerfile .
PAUSE