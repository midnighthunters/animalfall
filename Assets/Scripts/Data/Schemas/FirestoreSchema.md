# AnimalFall Firestore Schema

## Collections

### `users/{uid}`
| Field         | Type   | Description                     |
|---------------|--------|---------------------------------|
| uid           | string | Firebase Auth UID               |
| displayName   | string | Player display name             |
| email         | string | Email address                   |
| avatarUrl     | string | Profile picture URL             |
| createdAt     | number | Unix timestamp (ms)             |
| lastLoginAt   | number | Unix timestamp (ms)             |

### `users/{uid}/progress/data`
| Field                  | Type   | Description                |
|------------------------|--------|----------------------------|
| highestCompletedLevel  | number | Highest level beaten       |
| totalCoins             | number | Lifetime coins earned      |
| totalScore             | number | Lifetime score             |
| gamesPlayed            | number | Total games played         |
| lastPlayedAt           | number | Unix timestamp (ms)        |

### `users/{uid}/inventory/items`
| Field          | Type   | Description                   |
|----------------|--------|-------------------------------|
| powerUps       | array  | List of {powerUpId, quantity} |
| unlockedSkins  | array  | List of skin IDs              |
| gems           | number | Premium currency count        |

### `leaderboard/{uid}`
| Field        | Type   | Description              |
|--------------|--------|--------------------------|
| uid          | string | Player UID               |
| displayName  | string | Player name              |
| highScore    | number | Best single-game score   |
| highestLevel | number | Highest level reached    |
| updatedAt    | number | Unix timestamp (ms)      |

## Security Rules (recommended)

```javascript
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    match /users/{uid} {
      allow read, write: if request.auth != null && request.auth.uid == uid;
      
      match /progress/{doc} {
        allow read, write: if request.auth != null && request.auth.uid == uid;
      }
      
      match /inventory/{doc} {
        allow read, write: if request.auth != null && request.auth.uid == uid;
      }
    }
    
    match /leaderboard/{uid} {
      allow read: if request.auth != null;
      allow write: if request.auth != null && request.auth.uid == uid;
    }
  }
}
```
