cd $PSScriptRoot

$convert = "C:\Program Files (x86)\ImageMagick\convert.exe"
$ResourcesFolder = "DVBTTelevizor\DVBTTelevizor.Android\Resources"


$SourceImage = "Graphics\icon.png"
$SourceImageName = [System.IO.Path]::GetFileName($SourceImage)


& $convert -size 256x256 $SourceImage -resize 256x256 $ResourcesFolder\mipmap-anydpi-v26\$SourceImageName
& $convert -size 256x256 $SourceImage -resize 256x256 $ResourcesFolder\drawable\$SourceImageName
& $convert -size 192x192 $SourceImage -resize 192x192 $ResourcesFolder\mipmap-xxxhdpi\$SourceImageName
& $convert -size 144x144 $SourceImage -resize 144x144 $ResourcesFolder\mipmap-xxhdpi\$SourceImageName
& $convert -size 96x96   $SourceImage -resize 96x96   $ResourcesFolder\mipmap-xhdpi\$SourceImageName
& $convert -size 72x72   $SourceImage -resize 72x72   $ResourcesFolder\mipmap-hdpi\$SourceImageName
& $convert -size 48x48   $SourceImage -resize 48x48   $ResourcesFolder\mipmap-mdpi\$SourceImageName

$SourceImage = "Graphics\launcher_foreground.png"  
$SourceImageName = [System.IO.Path]::GetFileName($SourceImage)

& $convert -size 432x432 $SourceImage -resize 432x432 $ResourcesFolder\mipmap-xxxhdpi\$SourceImageName
& $convert -size 324x324 $SourceImage -resize 324x324 $ResourcesFolder\mipmap-xxhdpi\$SourceImageName
& $convert -size 216x216 $SourceImage -resize 216x216 $ResourcesFolder\mipmap-xhdpi\$SourceImageName
& $convert -size 162x162 $SourceImage -resize 162x162 $ResourcesFolder\mipmap-hdpi\$SourceImageName
& $convert -size 108x108 $SourceImage -resize 108x108 $ResourcesFolder\mipmap-mdpi\$SourceImageName
