import { IDropdownOption } from 'office-ui-fabric-react/lib/Dropdown';

import { IPlan } from './IPlan';

export interface IPlansDropdownOption extends IDropdownOption {
    plan: IPlan | null;
}
