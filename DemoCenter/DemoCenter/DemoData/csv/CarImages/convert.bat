for /f %%f in ('dir /b *.png') do "C:\Program Files\ImageMagick-7.1.1-Q16\magick.exe" convert -quality 75 -colors 16 %%f %%f