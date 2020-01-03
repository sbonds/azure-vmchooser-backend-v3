provider "kubernetes" {

}

resource "kubernetes_service" "backend" {
  metadata {
    generate_name = "vmchooserbackend"
  }
  spec {
    selector = {
      workload = "vmchooserbackend"
    }
    session_affinity = "ClientIP"
    port {
      port        = 80
      target_port = 80
    }

    type = "LoadBalancer"
  }
}

resource "kubernetes_deployment" "backend" {
  metadata {
    generate_name = "vmchooserbackend"
    namespace = "default"
    labels = {
      workload = "vmchooserbackend"
    }
  }

  spec {
    replicas = 3

    selector {
      match_labels = {
        workload = "vmchooserbackend"
      }
    }

    template {
      metadata {
        labels = {
          workload = "vmchooserbackend"
        }
      }

      spec {
        container {
          image = "vmchooserregistry.azurecr.io/vmchooser/backendv3:preview"
          name  = "vmchooserbackend"
          port {
            container_port = 80
          }
        }
      }
    }
  }
}