# Deployment with Kubernetes

Use the templates in this directory to deploy the app on a K8s cluster.

For an actual production environment, ensure that passwords are defined securely.
It is also recommended to use a suitable network storage for persistent volumes that is accessible from different nodes by the backend and worker containers.
Otherwise your data may get lost between restarts of the node hosting the data.



## 📦 Deployment

This is an outline of the deployment process for the Minikube cluster.

### Prerequisites

- [Minikube](https://minikube.sigs.k8s.io/docs/start/)
- [kubectl](https://kubernetes.io/docs/tasks/tools/)

Start Minikube according to your system needs.
Windows example:

```bash
minikube start --driver=hyperv --cpus=4 --memory=8192
```

### Prepare Docker Images

Docker images built locally:

```bash
docker build -t photo-mosaic-backend:latest ../backend
docker build -t photo-mosaic-frontend:latest ../frontend
docker build -t photo-mosaic-processing:latest ../processing
```

Then load them into Minikube:

```bash
minikube image load photo-mosaic-backend:latest
minikube image load photo-mosaic-frontend:latest
minikube image load photo-mosaic-processing:latest
```

### 🚀 Deploy Everything

```bash
kubectl apply -k ./
```

### 🧠 Optional: Verify
```bash
kubectl get pods -n photo-mosaic
kubectl get svc -n photo-mosaic
kubectl get pv -n photo-mosaic
```

### 🌐 Access the App

Expose the frontend via Minikube:

```bash
minikube service frontend -n photo-mosaic
```

This opens your browser to the running app.


### ↕️ Scaling Up / Down

Scale workers:

```bash
kubectl scale deployment worker -n photo-mosaic --replicas=3
```

Scale all deployments down:

```bash
kubectl scale deployment backend frontend python-api worker -n photo-mosaic --replicas=0
```


### 🧹 Tear Down

Warning: Your data will be lost! You might want to try `kubectl scale deployment backend frontend python-api worker -n photo-mosaic --replicas=0` instead.

```bash
kubectl delete namespace photo-mosaic
kubectl delete pv db-pv mosaic-pv
```
