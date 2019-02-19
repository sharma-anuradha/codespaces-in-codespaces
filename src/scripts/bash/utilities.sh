#!/bin/bash

# saner programming env: these switches turn some bugs into errors
set -o errexit -o pipefail -o noclobber -o nounset

color_debug="\e[1;30m"      # dark gray
color_error="\e[0;31m"      # red
color_verbose="\e[1;36m"    # cyan
color_warning="\e[1;33m"    # yellow
color_normal="\e[0m"        # (unset)

function init_utilities()
{
    verbose=0
    debug=0
    short_options=""
    long_options=""
}

function set_verbose()
{
    verbose=$1
}

function get_verbose()
{
    if [ $verbose -eq 1 ] ; then
        return 0
    fi
    return 1
}

function set_debug()
{
    debug=$1
}

function get_debug()
{
    if [ $debug -eq 1 ]; then
        return 0
    fi
    return 1
}

function add_option()
{
    local long=
    local short=

    if [ ! -z ${1+x} ]; then
        long="${1}"
    fi
    if [ ! -z ${2+x} ]; then
        short="${2}"
    fi

    if [ ! -z ${long} ]; then
        if [ -z ${long_options} ]; then
            long_options="${long}"
        else
            long_options="${long_options},${long}"
        fi
    fi

    if [ ! -z ${short} ]; then
        if [ -z ${short_options} ]; then
            short_options="${short}"
        else
            short_options="${short_options},${short}"
        fi
    fi
}

function get_short_options()
{
    echo "${short_options}"
}

function get_long_options()
{
    echo "${long_options}"
}

function echo_warning()
{
    local message=
    if [ ! -z ${1+x} ]; then
        message="Warning: $1"
    fi
    echo -e "${color_warning}${message}${color_normal}"
}

function echo_error()
{
    local message=
    if [ ! -z ${1+x} ]; then
        message="Error: $1"
    fi
    echo -e "${color_error}${message}${color_normal}" >&2
}

function echo_verbose()
{
    get_verbose || return 0
    local message=
    if [ ! -z ${1+x} ]; then
        message="Verbose: $1"
    fi
    echo -e "${color_verbose}${message}${color_normal}"
}

function echo_debug()
{
    get_debug || return 0
    local message=
    if [ ! -z ${1+x} ]; then
        message="Debug: $1"
    fi
    echo -e "${color_debug}${message}${color_normal}"
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

init_utilities

# testing
if [ ! -z ${1+x} ]; then
    if [ "${1}" == "test" ]; then
        test_utilities
        init_utilities
    fi
fi
