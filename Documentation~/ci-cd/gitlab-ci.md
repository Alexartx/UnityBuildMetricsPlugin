# GitLab CI Integration

Quick guide for integrating Build Metrics with GitLab CI/CD.

## Quick Start

### Step 1: Add Variable

```
Project → Settings → CI/CD → Variables → Expand → Add variable

Key: BUILD_METRICS_API_KEY
Value: bm_your_api_key_here
Flags: [x] Protect variable
       [x] Mask variable
```

### Step 2: Update .gitlab-ci.yml

```yaml
# .gitlab-ci.yml
variables:
  BUILD_METRICS_API_KEY: $BUILD_METRICS_API_KEY

build:
  image: unityci/editor:2022.3-android-1
  stage: build
  script:
    - unity-editor \
        -quit -batchmode -nographics \
        -projectPath . \
        -executeMethod YourBuildMethod.Build
  artifacts:
    paths:
      - build/
```

### Step 3: Push and Verify

```bash
git add .gitlab-ci.yml
git commit -m "Add Build Metrics integration"
git push
```

View results in [dashboard](https://app.buildmetrics.moonlightember.com).

---

## Complete Example

```yaml
# .gitlab-ci.yml
image: unityci/editor:2022.3.20f1-android-1

variables:
  BUILD_METRICS_API_KEY: $BUILD_METRICS_API_KEY
  GIT_LFS_SKIP_SMUDGE: "1"

stages:
  - build
  - deploy

before_script:
  - git lfs pull

build-android:
  stage: build
  script:
    - unity-editor \
        -quit -batchmode -nographics \
        -projectPath . \
        -buildTarget Android \
        -executeMethod YourBuildMethod.BuildAndroid \
        -logFile -
  artifacts:
    paths:
      - build/Android/
    expire_in: 1 week
  only:
    - main
    - develop

build-ios:
  stage: build
  tags:
    - macos
  script:
    - /Applications/Unity/Hub/Editor/2022.3.20f1/Unity.app/Contents/MacOS/Unity \
        -quit -batchmode \
        -projectPath . \
        -buildTarget iOS \
        -executeMethod YourBuildMethod.BuildiOS
  artifacts:
    paths:
      - build/iOS/
  only:
    - main
```

---

## Self-Hosted Runners

For self-hosted GitLab Runners:

```yaml
build:
  tags:
    - unity
    - macos
  script:
    - /path/to/Unity \
        -quit -batchmode \
        -projectPath . \
        -executeMethod YourBuildMethod.Build
  variables:
    BUILD_METRICS_API_KEY: $BUILD_METRICS_API_KEY
```

---

## See Also

- [Configuration Guide](../configuration.md)
- [GitHub Actions Guide](github-actions.md)
- [Jenkins Guide](jenkins.md)
