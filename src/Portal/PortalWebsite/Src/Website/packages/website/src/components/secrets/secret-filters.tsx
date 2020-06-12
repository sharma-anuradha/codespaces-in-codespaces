import React, { useCallback, useEffect } from 'react';

import {
    TextField,
    Stack,
    IconButton,
    Dropdown,
    IDropdownOption,
    ActionButton,
    Label,
    IIconProps,
} from 'office-ui-fabric-react';
import { useTranslation } from 'react-i18next';

import { FilterType, ISecretFilter } from 'vso-client-core';
import { getFilterDisplayName } from '../../utils/secretsUtils';

interface IFiltersListProps {
    filters: ISecretFilter[];
    saveFilters: (filters: ISecretFilter[]) => void;
    isFiltersValid: boolean;
    setIsFiltersValid: (valid: boolean) => void;
    validateOnLoad: boolean;
}

export function FiltersList(props: IFiltersListProps) {
    const { t: translation } = useTranslation();

    useEffect(() => {
        if (props.validateOnLoad) {
            validateFilters(props.filters);
        }
    }, [props.filters, props.validateOnLoad]);

    const getAvailebleFilterTypes = useCallback((): FilterType[] => {
        // Creates an array of all possible FilterType enum values
        const allFilterTypes: FilterType[] = Object.keys(FilterType).map(
            (x) => FilterType[x as keyof typeof FilterType]
        );

        // Remove the used filter types from the array
        props.filters.forEach((filter) => {
            const indexToRemove = allFilterTypes.findIndex((x) => x == filter.type);
            if (indexToRemove != -1) {
                allFilterTypes.splice(indexToRemove, 1);
            }
        });
        return allFilterTypes;
    }, [props.filters]);

    const addFilter = () => {
        const filterType = getAvailebleFilterTypes()[0];
        if (filterType) {
            const newFilter: ISecretFilter = {
                type: filterType,
                value: '',
            };
            const filtersToUpdate: ISecretFilter[] = [...props.filters, newFilter];
            props.saveFilters(filtersToUpdate);
            validateFilters(filtersToUpdate);
        }
    };

    const editFilter = (filter: ISecretFilter, filterType: FilterType, filterValue: string) => {
        const filterIndex = props.filters.findIndex((f) => f == filter);
        if (filterIndex != -1) {
            const filterCopy = { ...props.filters[filterIndex] };
            filterCopy.type = filterType;
            filterCopy.value = filterValue;
            const filtersToUpdate = props.filters.map((filter, index) =>
                index == filterIndex ? filterCopy : filter
            );
            props.saveFilters(filtersToUpdate);
            validateFilters(filtersToUpdate);
        }
    };

    const removeFilter = (filter: ISecretFilter) => {
        const filterIndex = props.filters.findIndex((f) => f == filter);
        if (filterIndex != -1) {
            const filtersToUpdate = props.filters.filter((_filter, index) => index != filterIndex);
            props.saveFilters(filtersToUpdate);
            validateFilters(filtersToUpdate);
        }
    };

    const validateFilters = (filtersList: ISecretFilter[]) => {
        let isValid: boolean = true;
        for (const filter of filtersList) {
            if (!filter.value?.length || filter.value.length > 200) {
                isValid = false;
                break;
            }
        }
        props.setIsFiltersValid(isValid);
    };

    return (
        <>
            <Label>{translation('secretFiltersTitle')}</Label>
            <ActionButton
                iconProps={{ iconName: 'Add' }}
                disabled={!getAvailebleFilterTypes().length}
                onClick={addFilter}
                ariaLabel={translation('secretLabelForAddFilter')}
            >
                {translation('secretLabelForAddFilter')}
            </ActionButton>
            {props.filters.map((filter) => (
                <SecretFilter
                    filter={filter}
                    getAvailebleFilterTypes={getAvailebleFilterTypes}
                    editFilter={editFilter}
                    removeFilter={removeFilter}
                    key={filter.type}
                />
            ))}
            {!props.isFiltersValid && (
                <div className='validation-error-message'>
                    <span>{translation('secretFilterError')}</span>
                </div>
            )}
        </>
    );
}

interface ISecretFilterProps {
    filter: ISecretFilter;
    getAvailebleFilterTypes: () => FilterType[];
    editFilter: (filter: ISecretFilter, filterType: FilterType, filterValue: string) => void;
    removeFilter: (filter: ISecretFilter) => void;
}

function SecretFilter(props: ISecretFilterProps) {
    const { t: translation } = useTranslation();
    const options: IDropdownOption[] = [
        { key: props.filter.type, text: getFilterDisplayName(props.filter.type, translation) },
    ];
    props.getAvailebleFilterTypes().forEach((filterType) => {
        options.push({ key: filterType, text: getFilterDisplayName(filterType, translation) });
    });

    const editFilter = (filter: ISecretFilter, filterType: FilterType, filterValue: string): void =>
        props.editFilter(filter, filterType, filterValue);

    return (
        <Stack horizontal>
            <Dropdown
                options={options}
                className='filter__dropdown'
                defaultSelectedKey={props.filter.type}
                onChange={(_event, option, _index) =>
                    editFilter(props.filter, option?.key as FilterType, props.filter.value)
                }
                required={true}
                ariaLabel={translation('secretLabelForFilterType')}
            ></Dropdown>
            <TextField
                placeholder={translation('secretPlaceholderForFilterValue')}
                className='filter__text'
                required
                value={props.filter.value}
                onChange={(_event, newFilterValue) =>
                    editFilter(props.filter, props.filter.type, newFilterValue ?? '')
                }
                ariaLabel={translation('secretLabelForFilterValue')}
            ></TextField>
            <IconButton
                iconProps={{ iconName: 'Cancel' }}
                title={translation('secretDeleteFilter')}
                onClick={(_event) => props.removeFilter(props.filter)}
                ariaLabel={translation('secretDeleteFilter')}
            />
        </Stack>
    );
}

interface IReadonlyFiltersListProps {
    item?: ISecretFilter;
}

export function ReadonlyFiltersList(props: IReadonlyFiltersListProps) {
    const { t: translation } = useTranslation();
    if (props.item) {
        return (
            <div data-is-focusable>
                <Label className='secrets-list__filter-item'>
                    {getFilterDisplayName(props.item.type, translation)}
                </Label>
                <span className='secrets-list__filter-item italic'>{props.item.value}</span>
            </div>
        );
    }
    return <></>;
}
