using 'main.bicep'

param location = 'eastus'
param namePrefix = 'noticesaas'
param sqlAdminLogin = 'noticesaasadmin'
// Supply at deploy time: az deployment group create ... --parameters jwtSigningKey=... sqlAdminPassword=...
param sqlAdminPassword = ''
param jwtSigningKey = ''
param apiImage = 'YOUR_ACR.azurecr.io/noticesaas-api:latest'
param webImage = 'YOUR_ACR.azurecr.io/noticesaas-web:latest'
