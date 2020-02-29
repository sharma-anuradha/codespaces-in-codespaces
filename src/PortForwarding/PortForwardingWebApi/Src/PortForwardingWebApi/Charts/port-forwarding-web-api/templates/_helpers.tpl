{{/* vim: set filetype=mustache: */}}
{{/*
Expand the name of the chart.
*/}}
{{- define "port-forwarding-web-api.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{/*
Create a default fully qualified app name.
We truncate at 63 chars because some Kubernetes name fields are limited to this (by the DNS naming spec).
If release name contains chart name it will be used as a full name.
*/}}
{{- define "port-forwarding-web-api.fullname" -}}
{{- if contains .Chart.Name .Release.Name -}}
{{- .Release.Name | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- printf "%s-%s" .Release.Name .Chart.Name | trunc 63 | trimSuffix "-" -}}
{{- end -}}
{{- end -}}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "port-forwarding-web-api.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{/*
Common labels
*/}}
{{- define "port-forwarding-web-api.labels" -}}
app.kubernetes.io/name: {{ include "port-forwarding-web-api.name" . }}
helm.sh/chart: {{ include "port-forwarding-web-api.chart" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end -}}

{{/*
Create the name of the service account to use
*/}}
{{- define "port-forwarding-web-api.serviceAccountName" -}}
{{- if .Values.serviceAccount.create -}}
    {{ default (include "port-forwarding-web-api.fullname" .) .Values.serviceAccount.name }}
{{- else -}}
    {{ default "default" .Values.serviceAccount.name }}
{{- end -}}
{{- end -}}

{{/*
Create the name of the role to use
*/}}
{{- define "port-forwarding-web-api.role" -}}
{{- if .Values.role.create -}}
    {{ default (include "port-forwarding-web-api.fullname" .) .Values.role.name }}
{{- else -}}
    {{ default "default" .Values.role.name }}
{{- end -}}
{{- end -}}

{{/*
Create the name of the rolebinding to use
*/}}
{{- define "port-forwarding-web-api.rolebinding" -}}
{{- if .Values.role.create -}}
    {{ default (include "port-forwarding-web-api.fullname" .) .Values.role.name }}
{{- else -}}
    {{ default "default" .Values.role.name }}
{{- end -}}
{{- end -}}