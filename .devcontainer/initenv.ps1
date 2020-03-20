wget https://raw.githubusercontent.com/microsoft/artifacts-credprovider/master/helpers/installcredprovider.ps1 -o ./installcredprovider.ps1 
./installcredprovider.ps1 -AddNetfx   
remove-item .\installcredprovider.ps1


wget https://dot.net/v1/dotnet-install.ps1 -o ./dotnet-install.ps1 
./dotnet-install.ps1 -v 3.1.200  -InstallDir 'C:\Program Files\dotnet' 
remove-item ./dotnet-install.ps1 