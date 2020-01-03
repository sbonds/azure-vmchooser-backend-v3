variable "workload" {
  type        = string
  default     = "vmchooser"
  description = "The workload we are deploying"
}

variable "enviromment" {
  type        = string
  description = "The environment we are deploying"
}

variable "namespace" {
  type        = string
  description = "The k8s namespace to use"
}

variable "imagename" {
  type        = string
  default     = "vmchooser/backendv3"
  description = "The name of the container image"
}

variable "imagelabel" {
  type        = string
  description = "The label of the container image"
}
