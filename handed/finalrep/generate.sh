#!/bin/sh
echo "Cleaning"
# Clean build directory
rm -r build
echo "Building"
# Make a new one
mkdir build
# Markdown --> LaTeX
pandoc finalreport.md --from markdown+implicit_figures+link_attributes --template template.tex -s -o main.tex
pandoc finalreport-fr.md --from markdown+implicit_figures+link_attributes --template template.tex -s -o mainfr.tex
# Move the main.tex to the build area. No idea why pandoc can't put it there
# directly.
mv main.tex build/main.tex
mv mainfr.tex build/mainfr.tex
# Also include additional files (images...)
cp include/* build/
echo "Done."
