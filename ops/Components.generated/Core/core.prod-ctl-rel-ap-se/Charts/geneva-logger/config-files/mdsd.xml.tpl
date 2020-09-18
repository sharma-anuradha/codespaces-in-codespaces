<?xml version="1.0" encoding="utf-8"?>
<MonitoringManagement timestamp="2018-12-04T00:00:00.000Z" eventVersion="{{ .Values.config.AzSecPack_EventVersion }}" namespace="{{ .Values.config.AzSecPack_Namespace }}" version="1.0" >
    <Accounts>
        <Account moniker="{{ .Values.config.AzSecPack_DiagnosticsMoniker }}" isDefault="true" autoKey="false" />
        <Account moniker="{{ .Values.config.AzSecPack_SecurityMoniker }}" autoKey="false" />
        <Account moniker="{{ .Values.config.AzSecPack_AuditMoniker }}" autoKey="false" />
    </Accounts>

    <Management defaultRetentionInDays="90" eventVolume="Large" >
        <Identity>
            <IdentityComponent name="Tenant" envariable="TENANT" />
            <IdentityComponent name="Role" envariable="ROLE" />
            <IdentityComponent name="RoleInstance" envariable="ROLEINSTANCE" />
        </Identity>
        <AgentResourceUsage diskQuotaInMB="20000" />
    </Management>

    <Sources>
        <Source name="ifxauditapp" dynamic_schema="true" />
        <Source name="ifxauditmgmt" dynamic_schema="true" />
        <Source name="kubernetes" dynamic_schema="true" />
        <Source name="kubeprobe" dynamic_schema="true" />
        <Source name="envreg" dynamic_schema="true" />
        <Source name="computeprovisioning" dynamic_schema="true" />
        <Source name="ContainerPoolWorker" dynamic_schema="true" />
        <Source name="signlr" dynamic_schema="true" />
        <Source name="vsobi" dynamic_schema="true" />
    </Sources>

    <Events>
        <MdsdEvents>
            <MdsdEventSource source="ifxauditapp" queryDelay="PT1M">
                <RouteEvent eventName="AsmIfxAuditApp" duration="PT1M" storeType="CentralBond" priority="Normal" account="{{ .Values.config.AzSecPack_AuditMoniker }}" />
            </MdsdEventSource>
            <MdsdEventSource source="ifxauditmgmt" queryDelay="PT1M">
                <RouteEvent eventName="AsmIfxAuditMgmt" duration="PT1M" storeType="CentralBond" priority="Normal" account="{{ .Values.config.AzSecPack_AuditMoniker }}" />
            </MdsdEventSource>
            <MdsdEventSource source="kubernetes" queryDelay="PT1M">
                <RouteEvent eventName="DefaultEvent" duration="PT1M" storeType="CentralBond" priority="Normal" account="{{ .Values.config.AzSecPack_DiagnosticsMoniker }}" />
            </MdsdEventSource>
            <MdsdEventSource source="kubeprobe" queryDelay="PT1M">
                <RouteEvent eventName="KubeProbeActivity" duration="PT1M" storeType="CentralBond" priority="Normal" account="{{ .Values.config.AzSecPack_DiagnosticsMoniker }}" />
            </MdsdEventSource>
            <MdsdEventSource source="envreg" queryDelay="PT1M">
                <RouteEvent eventName="EnvRegActivity" duration="PT1M" storeType="CentralBond" priority="Normal" account="{{ .Values.config.AzSecPack_DiagnosticsMoniker }}" />
            </MdsdEventSource>
            <MdsdEventSource source="computeprovisioning" queryDelay="PT1M">
                <RouteEvent eventName="ComputeProvActivity" duration="PT1M" storeType="CentralBond" priority="Normal" account="{{ .Values.config.AzSecPack_DiagnosticsMoniker }}" />
            </MdsdEventSource>
            <MdsdEventSource source="ContainerPoolWorker" queryDelay="PT1M">
                <RouteEvent eventName="ContainerPoolActivity" duration="PT1M" storeType="CentralBond" priority="Normal" account="{{ .Values.config.AzSecPack_DiagnosticsMoniker }}" />
            </MdsdEventSource>
            <MdsdEventSource source="signlr" queryDelay="PT1M">
                <RouteEvent eventName="SignlrActivity" duration="PT1M" storeType="CentralBond" priority="Normal" account="{{ .Values.config.AzSecPack_DiagnosticsMoniker }}" />
            </MdsdEventSource>
            <MdsdEventSource source="vsobi" queryDelay="PT1M">
                <RouteEvent eventName="VsoBiActivity" duration="PT1M" storeType="CentralBond" priority="Normal" account="{{ .Values.config.AzSecPack_DiagnosticsMoniker }}" />
            </MdsdEventSource>
        </MdsdEvents>
    </Events>
</MonitoringManagement>
