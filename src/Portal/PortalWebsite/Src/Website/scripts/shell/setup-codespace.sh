#!/bin/bash

# Normal Text
export PALETTE_RESET='\e[0m'

# Dimmed text
export PALETTE_DIM='\e[2m'

# Bold Text
export PALETTE_BOLD='\e[1m'

# Underlined Text
export PALETTE_UNDERLINED='\e[4m'

# Blinking
export PALETTE_BLINK='\e[5m'

# Reverse
export PALETTE_REVERSE='\e[7m'

# Foreground Color
export PALETTE_BLACK='\e[30m'
export PALETTE_WHITE="\e[97m"
export PALETTE_RED='\e[31m'
export PALETTE_GREEN='\e[32m'
export PALETTE_BROWN='\e[33m'
export PALETTE_BLUE='\e[34m'
export PALETTE_PURPLE='\e[35m'
export PALETTE_CYAN='\e[36m'
export PALETTE_LIGHTGRAY='\e[37m'
export PALETTE_LIGHT_YELLOW="\e[93m"

# Background Color
export PALETTE_BLACK_U='\e[40m'
export PALETTE_RED_U='\e[41m'
export PALETTE_GREEN_U='\e[42m'
export PALETTE_BROWN_U='\e[43m'
export PALETTE_BLUE_U='\e[44m'
export PALETTE_PURPLE_U='\e[45m'
export PALETTE_CYAN_U='\e[46m'
export PALETTE_LIGHTGRAY_U='\e[47m'

GREETINGS=("Bonjour" "Hello" "Salam" "Привет" "Вітаю" "Hola" "Zdravo" "Ciao" "Salut" "Hallo" "Nǐ hǎo" "Xin chào" "Yeoboseyo" "Aloha" "Namaskaram" "Wannakam" "Dzień dobry")
GREETING=${GREETINGS[$RANDOM % ${#GREETINGS[@]} ]}

# if called not from the global setup script, show the greeting
if ! [ -z ${VSCS_SETUP_PREVENT_WEBSITE_GREETING} ]; then
    echo -e $PALETTE_WHITE"\n
        ~+

                 *       +
           '                  |
         +   .-.,=\"\`\`\"=.    - o -
             '=/_       \     |
          *   |  '=._    |   
               \     \`=./\`,        '
            .   '=.__.=' \`='      *
   +                         +
        O      *        '       .
"$PALETTE_RESET

echo -e $PALETTE_GREEN"\n\n     🖖 👽  $GREETING, Codespacer 👽 🖖\n"$PALETTE_RESET
sleep 1s

fi

echo -e $PALETTE_CYAN"\n- Get NPM modules from AzDO\n"$PALETTE_RESET

if [ -z "$AZ_DO_PAT" ]; then
    stty_orig=$(stty -g)
    stty -echo
    read -p " ↳ Provide AzDO PAT for private NPM feeds: " AZ_DO_PAT
    stty $stty_orig
    echo ""
    if [ -z "$AZ_DO_PAT" ]; then
        echo -e $PALETTE_RED"\n   🐢 No PAT - Zero FLOPS per watt"$PALETTE_RESET
        exit 1
    else
        AZ_DO_PAT_BASE64=$(echo -n $AZ_DO_PAT | base64)
        echo "
            export AZ_DO_PAT=$AZ_DO_PAT
            export AZ_DO_PAT_BASE64=$AZ_DO_PAT_BASE64
        " >> ~/.bashrc
    fi
fi

echo -e $PALETTE_LIGHT_YELLOW" ⌬ Installing NPM dependencies...\n"$PALETTE_RESET

# the unset issue to fix the issue where `yarn` cannot pick up the `~/.npmrc`
# for private feeds auth when run from within an package.json script
cd $CSCLIENT
unset $(env | awk -F= '$1 ~ /^npm_/ {print $1}')
yarn install

echo -e $PALETTE_CYAN"\n- Get NuGet modules from AzDO"$PALETTE_RESET

echo -e $PALETTE_LIGHT_YELLOW"\n ⌬ Installing NuGet dependencies...\n"$PALETTE_RESET

# invoke dotnet restore interactively to trigger Azure Credentials Provider auth
cd $CSSERVER && dotnet restore --interactive

exec

cd $CSCLIENT

exec

echo -e $PALETTE_GREEN"\n\n     🖖 👽  [✔] All done, Happy SpaceCoding! 👽 🖖\n"$PALETTE_RESET
