set -e  # stop on any error

echo "=== Building Tailwind + DaisyUI ==="
cd wwwroot/vendors
npm run build:css
cd ../..

echo "=== Publishing OnlyPaws ==="
sudo dotnet publish -c Release -f net9.0 # -o /opt/onlypaws --no-restore


