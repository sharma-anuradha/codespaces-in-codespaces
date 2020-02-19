import { SelectableOptionMenuItemType } from 'office-ui-fabric-react/lib/utilities/selectableOption/SelectableOption.types';
import { IPlansDropdownOption } from '../../interfaces/IPlansDropdownOption';

export const createNewPlanKey = 'Create-Plan';

export const createNewPlanDropdownOption = {
    key: createNewPlanKey,
    text: '• Create new plan',
    plan: null,
} as IPlansDropdownOption;

export const dividerDropdownOption = {
    key: 'divider-key',
    text: '',
    plan: null,
    disabled: true,
    itemType: SelectableOptionMenuItemType.Divider,
};
