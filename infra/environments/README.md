# Terraform Environments

This folder contains isolated Terraform root modules for each environment.

## Environments

- `dev`
- `prod`

Each environment has its own:

- backend state key
- `terraform.tfvars`
- root module entrypoint

The `main.tf`, `variables.tf`, and `outputs.tf` files intentionally remain similar between environments. The reusable infrastructure logic lives in `infra/modules/core`.

Environment-specific differences should usually go in `terraform.tfvars`, not in the module code.
