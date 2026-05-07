module "core" {
  source = "../../modules/core"

  subscription_id = var.subscription_id
  location        = var.location
  environment     = var.environment
  project_name    = var.project_name
}