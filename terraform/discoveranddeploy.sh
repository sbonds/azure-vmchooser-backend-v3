#!/bin/bash
clientid=$1
clientsecret=$2
subscriptionid=$3
tenantid=$4
environment=$5
workload=$6

# login
az login --service-principal -u "$clientid" -p "$clientsecret" --tenant "$tenantid"
az account set -s "$subscriptionid"
az extension add --name resource-graph

# functions
function getClusterName {
  temp=`az graph query -q "Resources | where type =~ \"Microsoft.ContainerService/ManagedClusters\" | where properties.provisioningState =~ \"Succeeded\" | where tags[\"Deployment\"] =~ \"$1\" | where tags[\"Environment\"] =~ \"$environment\" | where tags[\"Workload\"] =~ \"$workload\" | project name" -o yaml | awk '{ print $3 }'`
  echo $temp
}

function getClusterResourcegroup {
  temp=`az graph query -q "Resources | where type =~ \"Microsoft.ContainerService/ManagedClusters\" | where name =~ \"$1\" | project resourceGroup" -o yaml | awk '{ print $3 }'`
  echo $temp
}

function getClusterKubectl { 
  temp=`az aks get-credentials -g $2 -n $1`
  echo $temp
}

function setClusterTags {
  temp=`az resource tag --resource-group $2 --name $1 --resource-type "Microsoft.ContainerService/ManagedClusters" --tags "Deployment=$3" "Workload=$4" "Environment=$5"`
}

function deployToCluster {
  echo "Deploying to $1"
  # TODO : ADD TF Commands / more parameters / etc
}

# main runtime
## Install kubectl
sudo az aks install-cli
export PATH=$PATH:/usr/local/bin

## Discover Clusters in their various stages
created=$(getClusterName "Created")  
active=$(getClusterName "Active")
deprecated=$(getClusterName "Deprecated")
deprecate=false

echo "Looking for newly created clusters"
for clustername in $created
do
  clusterrg=$(getClusterResourcegroup $clustername)
  getClusterKubectl $clustername $clusterrg
  deployToCluster $1
  echo "Setting $clustername to Ready"
  setClusterTags $clustername $clusterrg "Ready" $workload $enviromnent
  deprecate=true
done

echo "Looking for Deprecated clusters"
for clustername in $deprecated
do
  clusterrg=$(getClusterResourcegroup $clustername)
  getClusterKubectl $clustername $clusterrg
  deployToCluster $1
done

echo "Looking for Active clusters"
for clustername in $active
do
  clusterrg=$(getClusterResourcegroup $clustername)
  getClusterKubectl $clustername $clusterrg
  deployToCluster $1
  if (deprecate)
    echo "Deprecating $clustername"
    setClusterTags $clustername $clusterrg "Deprecated" $workload $enviromnent
  fi
done