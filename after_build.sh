rm -rf BuildOutput &&
mkdir BuildOutput &&
cp -f "$CURRENT_PROJECT"/bin/Debug/netstandard2.1/MoonsweptTeam."$CURRENT_PROJECT".dll BuildOutput/"$CURRENT_PROJECT".dll &&
cp -f "$CURRENT_PROJECT"/README.md BuildOutput/ &&
cp -f "$CURRENT_PROJECT"/CHANGELOG.md BuildOutput/ &&
cp -f "$CURRENT_PROJECT"/icon.png BuildOutput/ &&
cp -f AssetBundle/Moonswept BuildOutput/ &&
cp -f LICENSE BuildOutput/ &&
./generate_manifest.sh &&
./generate_zipfile.sh &&
dolphin "./BuildOutput"
