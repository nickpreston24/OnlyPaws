set -e  # stop on any error

echo "=== Building Tailwind + DaisyUI ==="
cd wwwroot/vendors
npm run build:css
cd ../..

sh ./overwrite_systemd_service.sh
sudo systemctl daemon-reload
sudo systemctl restart onlypaws

echo "=== Publishing OnlyPaws ==="
sudo dotnet publish -c Release -f net9.0 # -o /opt/onlypaws --no-restore

echo "Htmx exist at 'bin/Release/net9.0/publish/wwwroot/vendors/htmx/'?"
ls bin/Release/net9.0/publish/wwwroot/vendors/htmx/

echo "css exist?"
ls bin/Release/net9.0/publish/wwwroot/css/          # should show site.css + extra.css

echo "vendors htmx exist?"
ls bin/Release/net9.0/publish/vendors/htmx/        # should show htmx.min.js
