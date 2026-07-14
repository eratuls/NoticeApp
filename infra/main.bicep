@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Short name prefix (letters/numbers, 3-12 chars).')
@minLength(3)
@maxLength(12)
param namePrefix string = 'noticesaas'

@description('SQL administrator login.')
param sqlAdminLogin string

@secure()
@description('SQL administrator password.')
param sqlAdminPassword string

@description('Container image for the API (ACR or other registry).')
param apiImage string = 'mcr.microsoft.com/dotnet/samples:aspnetapp'

@description('Container image for the Angular web (nginx).')
param webImage string = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'

@description('JWT signing key (min 32 characters). Prefer Key Vault in real deploys.')
@secure()
@minLength(32)
param jwtSigningKey string

var sqlServerName = '${namePrefix}-sql-${uniqueString(resourceGroup().id)}'
var sqlDbName = 'NoticeSaaS'
var storageName = take(replace('${namePrefix}st${uniqueString(resourceGroup().id)}', '-', ''), 24)
var logAnalyticsName = '${namePrefix}-logs'
var envName = '${namePrefix}-cae'
var apiAppName = '${namePrefix}-api'
var webAppName = '${namePrefix}-web'
var blobContainerName = 'notice-attachments'

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource sqlFirewallAzure 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDb 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: sqlDbName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
    capacity: 5
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource blobServices 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storage
  name: 'default'
}

resource attachmentsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobServices
  name: blobContainerName
  properties: {
    publicAccess: 'None'
  }
}

resource containerAppEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: envName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

var sqlConnectionString = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDbName};Persist Security Info=False;User ID=${sqlAdminLogin};Password=${sqlAdminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
var storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'

resource apiApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: apiAppName
  location: location
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
        allowInsecure: false
      }
      secrets: [
        {
          name: 'sql-connection'
          value: sqlConnectionString
        }
        {
          name: 'jwt-signing-key'
          value: jwtSigningKey
        }
        {
          name: 'blob-connection'
          value: storageConnectionString
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: apiImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:8080'
            }
            {
              name: 'ConnectionStrings__Default'
              secretRef: 'sql-connection'
            }
            {
              name: 'Auth__Jwt__SigningKey'
              secretRef: 'jwt-signing-key'
            }
            {
              name: 'Auth__Jwt__Issuer'
              value: 'NoticeSaaS'
            }
            {
              name: 'Auth__Jwt__Audience'
              value: 'NoticeSaaS.Web'
            }
            {
              name: 'Storage__Provider'
              value: 'AzureBlob'
            }
            {
              name: 'Storage__AzureBlob__ConnectionString'
              secretRef: 'blob-connection'
            }
            {
              name: 'Storage__AzureBlob__ContainerName'
              value: blobContainerName
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
      }
    }
  }
}

resource webApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: webAppName
  location: location
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: 80
        transport: 'http'
        allowInsecure: false
      }
    }
    template: {
      containers: [
        {
          name: 'web'
          image: webImage
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
      }
    }
  }
  dependsOn: [
    apiApp
  ]
}

output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output sqlDatabaseName string = sqlDbName
output storageAccountName string = storage.name
output blobContainerNameOut string = blobContainerName
output apiFqdn string = apiApp.properties.configuration.ingress.fqdn
output webFqdn string = webApp.properties.configuration.ingress.fqdn
