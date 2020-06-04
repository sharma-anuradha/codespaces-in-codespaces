import { SelectableOptionMenuItemType } from 'office-ui-fabric-react/lib/utilities/selectableOption/SelectableOption.types';
import { IPlansDropdownOption } from '../../interfaces/IPlansDropdownOption';
import { TFunction } from 'i18next';

export const createNewPlanKey = 'Create-Plan';

export function createNewPlanDropdownOption(translationFunc: TFunction) {
    return {
        key: createNewPlanKey,
        text: `• ${translationFunc('createNewPlan')}`,
        plan: null,
    } as IPlansDropdownOption
}

export const dividerDropdownOption = {
    key: 'divider-key',
    text: '',
    plan: null,
    disabled: true,
    itemType: SelectableOptionMenuItemType.Divider,
};
