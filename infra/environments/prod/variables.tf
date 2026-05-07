variable "subscription_id" {
  description = "Azure subscription ID where resources will be managed."
  type        = string
}

variable "location" {
  description = "Azure region for resources."
  type        = string
}

variable "environment" {
  description = "Environment name, for example dev, test, or prod."
  type        = string
}

variable "project_name" {
  description = "Project name used for naming and tagging Azure resources."
  type        = string
}