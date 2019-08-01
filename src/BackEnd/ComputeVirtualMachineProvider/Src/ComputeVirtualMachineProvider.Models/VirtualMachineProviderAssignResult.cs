using Common.Models;

namespace ComputeVirtualMachineProvider.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class VirtualMachineProviderAssignResult : BaseContinuationResult
    {

        /// <summary>
        /// 
        /// Example: '/subscriptions/2fa47206-c4b5-40ff-a5e6-9160f9ee000c/storage/<uid>'
        /// </summary>V
        public string ResourceId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string TrackingId { get; set; }
    }
}


/*

SKU CATALOG DEFINITION EXAMPLE

'CE_Small_Linux_1' ->
    Storage - Premium, 8GB
    Compute Family - F Series
    Compute Sku - FS4_v3
    CEUnits - 10
    OS - Linux
'CE_Medium_Linux_1' ->
    Storage - Premium, 32GB
    Compute Family - F Series
    Compute Sku - FS16_v3
    CEUnits - 20
    OS - Linux
'CE_Large_Linux_1' ->
    Storage - Premium, 64GB
    Compute Family - F Series
    Compute Sku - FS32_v3
    CEUnits - 30
    OS - Linux


*/
