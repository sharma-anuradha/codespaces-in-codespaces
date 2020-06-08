/**
 * API Version	Endpoint URL
 * 2020-05-26-preview	            online.visualstudio.com/api/v1
 * 2020-05-26-beta	                online-ppe.vsengsaas.visualstudio.com/api/v1
 * 2020-05-26-alpha	                online.dev.vsengsaas.visualstudio.com/api/v1
 * 2020-05-26-privatepreview	    canary.online.visualstudio.com/api/v1
 */
export const getAzureManagementApiVersion = (): string => {
    const baseURL = window.location.href.split('/')[2];
    let apiVersion;
    if (baseURL.includes('dev')) {
        apiVersion = '2020-05-26-alpha';
    } else if (baseURL.includes('ppe')) {
        apiVersion = '2020-05-26-beta';
    } else if (baseURL.includes('canary')) {
        apiVersion = '2020-05-26-privatepreview';
    } else {
        apiVersion = '2020-05-26-preview';
    }
    return apiVersion;
};
