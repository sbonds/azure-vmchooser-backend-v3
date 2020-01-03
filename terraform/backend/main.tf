provider "kubernetes" {

}

resource "kubernetes_namespace" "namespace" {
  metadata {
    name = var.namespace
  }
}

resource "kubernetes_deployment" "${var.workload}-backend" {
  metadata {
    name = var.imagename
    labels = {
      Workload = var.Workload
    }
  }

  spec {
    replicas = 3

      spec {
        container {
          image = "${var.imagename}:${var.imagelabel}"
          name  = "${var.workload}-backend"

          resources {
            limits {
              cpu    = "0.5"
              memory = "512Mi"
            }
            requests {
              cpu    = "250m"
              memory = "50Mi"
            }
          }

          liveness_probe {
            http_get {
              path = "/"
              port = 80
            }

          initial_delay_seconds = 3
          period_seconds        = 3
        }
      }
    }
  }
}