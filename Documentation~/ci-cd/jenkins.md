# Jenkins Integration

Quick guide for integrating Build Metrics with Jenkins.

## Quick Start

### Step 1: Add Credential

```
Manage Jenkins → Manage Credentials → (global) → Add Credentials

Kind: Secret text
Secret: bm_your_api_key_here
ID: build-metrics-api-key
Description: Build Metrics API Key
```

### Step 2: Configure Job

**Freestyle Project:**

```
Build Environment:
[x] Use secret text(s) or file(s)

Bindings:
Variable: BUILD_METRICS_API_KEY
Credentials: build-metrics-api-key

Build → Execute shell:
/path/to/Unity \
  -quit -batchmode -nographics \
  -projectPath . \
  -executeMethod YourBuildMethod.Build \
  -logFile -
```

**Pipeline (Jenkinsfile):**

```groovy
pipeline {
    agent any

    environment {
        BUILD_METRICS_API_KEY = credentials('build-metrics-api-key')
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Build') {
            steps {
                sh '''
                    /Applications/Unity/Hub/Editor/2022.3.20f1/Unity.app/Contents/MacOS/Unity \
                        -quit -batchmode -nographics \
                        -projectPath . \
                        -executeMethod BuildScripts.BuildAndroid \
                        -logFile -
                '''
            }
        }

        stage('Archive') {
            steps {
                archiveArtifacts artifacts: 'build/**/*', fingerprint: true
            }
        }
    }
}
```

### Step 3: Build and Verify

Run job → Check console output for:
```
[BuildMetrics] Build metrics sent successfully
```

View in [dashboard](https://app.buildmetrics.moonlightember.com).

---

## Multi-Platform Example

```groovy
pipeline {
    agent any

    environment {
        BUILD_METRICS_API_KEY = credentials('build-metrics-api-key')
        UNITY_PATH = '/Applications/Unity/Hub/Editor/2022.3.20f1/Unity.app/Contents/MacOS/Unity'
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
                sh 'git lfs pull'
            }
        }

        stage('Build') {
            parallel {
                stage('Android') {
                    steps {
                        sh """
                            ${UNITY_PATH} \
                                -quit -batchmode \
                                -projectPath . \
                                -buildTarget Android \
                                -executeMethod BuildScripts.BuildAndroid
                        """
                    }
                }

                stage('iOS') {
                    steps {
                        sh """
                            ${UNITY_PATH} \
                                -quit -batchmode \
                                -projectPath . \
                                -buildTarget iOS \
                                -executeMethod BuildScripts.BuildiOS
                        """
                    }
                }
            }
        }

        stage('Deploy') {
            steps {
                // Your deployment steps
                sh 'fastlane android beta'
            }
        }
    }

    post {
        success {
            archiveArtifacts artifacts: 'build/**/*'
        }
    }
}
```

---

## Windows Agents

For Windows agents:

```groovy
pipeline {
    agent { label 'windows' }

    environment {
        BUILD_METRICS_API_KEY = credentials('build-metrics-api-key')
        UNITY_PATH = 'C:\\Program Files\\Unity\\Hub\\Editor\\2022.3.20f1\\Editor\\Unity.exe'
    }

    stages {
        stage('Build') {
            steps {
                bat """
                    "${UNITY_PATH}" ^
                        -quit -batchmode -nographics ^
                        -projectPath . ^
                        -executeMethod BuildScripts.Build
                """
            }
        }
    }
}
```

---

## See Also

- [Configuration Guide](../configuration.md)
- [GitHub Actions Guide](github-actions.md)
- [GitLab CI Guide](gitlab-ci.md)
