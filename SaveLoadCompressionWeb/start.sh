#!/bin/sh

# The script replaces some values in the nginx configuration file
# based upon environment values set for the container
# After completion of the replacement it starts the nginx server.
# Often containers are hosted with port mappings with the host.
# This is will replace the placeholder with PORT value enviroment variable set in the 
# container in the nginx configuration file.
sed -i -E "s/TO_REPLACE_PORT/${PORT:-80}/" /etc/nginx/nginx.conf
# Below will replace the placeholder in the nginx congfiguration file
sed -i -e 's/TO_REPLACE_BLAZOR_ENVIRONMENT/'"$BLAZOR_ENVIRONMENT"'/g' /etc/nginx/nginx.conf 
# Start nginx service in the container
nginx -g 'daemon off;'
