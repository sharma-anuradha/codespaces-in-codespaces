#!/bin/bash

# saner programming env: these switches turn some bugs into errors
set -o errexit -o pipefail -o noclobber -o nounset

# Script PIN 6377 to avoid global name conflicts
script_name_6377="$( basename "${BASH_SOURCE}" )"

# For colors see https://stackoverflow.com/questions/5947742/how-to-change-the-output-color-of-echo-in-linux
utilities_color_info="\e[0;32m"       # green
utilities_color_debug="\e[1;30m"      # dark gray
utilities_color_error="\e[0;31m"      # red
utilities_color_verbose="\e[1;36m"    # cyan
utilities_color_warning="\e[1;33m"    # yellow
utilities_color_normal="\e[0m"        # (unset)

function init_utilities()
{
    utilities_init_flag=1
    utilities_verbose=0
    utilities_debug=0
    utilities_dry_run=0
    utilities_short_options=""
    utilities_long_options=""
}

function dump_utilities_vars()
{
    echo "utilities_init_flag=${utilities_init_flag}"
    echo "utilities_verbose=${utilities_verbose}"
    echo "utilities_debug=${utilities_debug}"
    echo "utilities_dry_run=${utilities_dry_run}"
    echo "utilities_dry_run=${utilities_short_options}"
    echo "utilities_long_options=${utilities_long_options}"
}

function set_verbose()
{
    echo_debug "${script_name_6377}::${FUNCNAME[0]} $@"
    utilities_verbose=$1
}

function get_verbose()
{
    if [ "$utilities_verbose" == "1" ] ; then
        return 0
    fi
    return 1
}

function set_debug()
{
    echo_debug "${script_name_6377}::${FUNCNAME[0]} $@"
    utilities_debug=$1
}

function get_debug()
{
    if [ "$utilities_debug" == "1" ]; then
        return 0
    fi
    return 1
}

function set_dry_run()
{
    echo_debug "${script_name_6377}::${FUNCNAME[0]} $@"
    utilities_dry_run=$1
}

function get_dry_run()
{
    if [ "$utilities_dry_run" == "1" ]; then
        return 0 # success
    fi
    return 1 # failure
}

function add_option()
{
    echo_debug "${script_name_6377}::${FUNCNAME[0]} $@"

    local long=
    local short=

    if [ ! -z ${1+x} ]; then
        long="${1}"
    fi
    if [ ! -z ${2+x} ]; then
        short="${2}"
    fi

    if [ ! -z ${long} ]; then
        if [ -z ${utilities_long_options} ]; then
            utilities_long_options="${long}"
        else
            utilities_long_options="${utilities_long_options},${long}"
        fi
    fi

    if [ ! -z ${short} ]; then
        if [ -z ${utilities_short_options} ]; then
            utilities_short_options="${short}"
        else
            utilities_short_options="${utilities_short_options},${short}"
        fi
    fi
}

function get_short_options()
{
    echo "${utilities_short_options}"
}

function get_long_options()
{
    echo "${utilities_long_options}"
}

function echo_warning()
{
    local message=
    if [ ! -z ${1+x} ]; then
        message="Warning: $1"
    fi
    echo -e "${utilities_color_warning}${message}${utilities_color_normal}"
}

function echo_info()
{
    local message=
    if [ ! -z ${1+x} ]; then
        message="$1"
    fi
    echo -e "${utilities_color_info}${message}${utilities_color_normal}" >&2
}


function echo_error()
{
    local message=
    if [ ! -z ${1+x} ]; then
        message="Error: $1"
    fi
    echo -e "${utilities_color_error}${message}${utilities_color_normal}" >&2
}

function echo_verbose()
{
    get_verbose || return 0
    local message=
    if [ ! -z ${1+x} ]; then
        message="VERBOSE: $1"
    fi
    echo -e "${utilities_color_verbose}${message}${utilities_color_normal}"
}

function echo_debug()
{
    get_debug || return 0
    local message=
    if [ ! -z ${1+x} ]; then
        message="DEBUG: $1"
    fi
    echo -e "${utilities_color_debug}${message}${utilities_color_normal}"
}

function exec_verbose()
{
    echo_debug "${script_name_6377}::${FUNCNAME[0]} $@"
    local command="$@"    
    echo_verbose "${command}"
    $command
}

function exec_dry_run()
{
    echo_debug "${script_name_6377}::${FUNCNAME[0]} $@"
    local command="$@"

    if get_dry_run; then
        echo_verbose "DRY-RUN: ${command}"
        return 0
    fi
    exec_verbose "${command}"
}

function exec_hide_stderr()
{
    get_dry_run && return 0
    local command="$@"
    $(${command}) 2> /dev/null
}

function test_utilities()
{
    echo "normal output"
    echo_warning "this is a warning"
    echo_error "this is an error"

    echo_verbose "this verbose message should not be printed"
    set_verbose 1
    echo_verbose "this verbose message should be printed"
    set_verbose 0
    echo_verbose "this verbose message should not be printed"

    echo_debug "this debug message should not be printed"
    set_debug 1
    echo_debug "this debug message should be printed"
    set_debug 0
    echo_debug "this debug message should not be printed"

    echo "get_long_options '$(get_long_options)' expected ''"
    echo "get_short_options '$(get_short_options)' expected ''"

    add_option "servicename:" "s:" 
    echo "get_long_options '$(get_long_options)' expected 'servicename:'"
    echo "get_short_options '$(get_short_options)' expected 's:'"

    add_option "opt"
    echo "get_long_options '$(get_long_options)' expected 'servicename:,opt'"
    echo "get_short_options'$(get_short_options)' expected 's:'"

    add_option "help" "h"
    echo "get_long_options '$(get_long_options)' expected 'servicename:,opt,help'"
    echo "get_short_options '$(get_short_options)' expected 's:,h'"
}

# Default initialization happens on first import
if [ -z ${utilities_init_flag+x} ]; then
    init_utilities
fi

# Testing
if [ ! -z ${1+x} ]; then
    if [ "${1}" == "test" ]; then
        test_utilities
        init_utilities
    fi
fi
