# Image Optimizer V2

An image optimization service that uses libvips for fast and efficient image processing.

## System Requirements

### Linux (Debian/Ubuntu)

```bash
sudo apt update
sudo apt install -y libvips-dev
```

### macOS

```bash
brew install vips
```

### Windows

Download and install vips from: https://www.libvips.org/install.html

## Docker Deployment (Recommended)

```bash
docker build -t image-optimizer .
docker run -p 8080:8080 image-optimizer
```
