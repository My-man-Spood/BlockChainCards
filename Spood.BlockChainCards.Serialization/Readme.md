# BlockChainCards.Serialization

## Binary Serialization Format: MintCardTransaction

This document describes the binary layout for serializing a `MintCardTransaction` in the BlockChainCards protocol. This format is designed for efficiency, clarity, and future-proofing.

### Field Layout (with Length-Prefixed Fields)

| Offset | Field                     | Size (bytes)      | Description                                      |
|--------|---------------------------|-------------------|--------------------------------------------------|
| 0      | version                   | 1                 | Format version (e.g. 0x01)                       |
| 1      | discriminator             | 1                 | Transaction type (e.g. 0x01)                     |
| 2      | authorityKeyLen           | 4                 | Length of authorityPublicKey (int, little-endian) |
| 6      | authorityPublicKey        | N (variable)      | Authority's public key (DER/PEM, etc.)           |
| 6+N    | recipientKeyLen           | 4                 | Length of recipientPublicKey                      |
| 10+N   | recipientPublicKey        | M (variable)      | Recipient's public key                            |
| 10+N+M | card                      | 32                | Card hash (fixed size)                            |
| ...    | timestamp                 | 8                 | DateTime.ToBinary()                               |
| ...    | authoritySignatureLen     | 4                 | Length of authoritySignature (int, little-endian) |
| ...    | authoritySignature        | S (variable)      | Signature bytes (if present)                      |

- All multi-byte fields are encoded in little-endian order.
- For each variable-length field (public keys, signature), write a 4-byte length prefix followed by the bytes.
- If `authoritySignature` is not present, write a length of 0 and no signature bytes.

## Binary Serialization Format: TradeCardsTransaction

| Offset | Field                  | Size (bytes)      | Description                                             |
|--------|------------------------|-------------------|---------------------------------------------------------|
| 0      | version                | 1                 | Format version (e.g. 0x01)                              |
| 1      | discriminator          | 1                 | Transaction type (0x02)                                 |
| 2      | user1KeyLen            | 4                 | Length of User1PublicKey (int, little-endian)           |
| 6      | user1PublicKey         | N (variable)      | DER-encoded User 1 public key                           |
| 6+N    | user2KeyLen            | 4                 | Length of User2PublicKey (int, little-endian)           |
| 10+N   | user2PublicKey         | M (variable)      | DER-encoded User 2 public key                           |
| 10+N+M | cardsFromUser1Count    | 4                 | Number of cards from User 1 (int, little-endian)        |
| ...    | cardsFromUser1         | 32 * C1           | C1 card hashes from User 1 (each 32 bytes)              |
| ...    | cardsFromUser2Count    | 4                 | Number of cards from User 2 (int, little-endian)        |
| ...    | cardsFromUser2         | 32 * C2           | C2 card hashes from User 2 (each 32 bytes)              |
| ...    | timestamp              | 8                 | DateTime.ToBinary()                                     |
| ...    | user1SignatureLen      | 4                 | Length of User1Signature (int, little-endian)           |
| ...    | user1Signature         | S1 (variable)     | DER-encoded ECDSA signature from User 1 (if present)    |
| ...    | user2SignatureLen      | 4                 | Length of User2Signature (int, little-endian)           |
| ...    | user2Signature         | S2 (variable)     | DER-encoded ECDSA signature from User 2 (if present)    |

- All multi-byte fields are little-endian.
- For each variable-length field, write a 4-byte length prefix followed by the bytes.
- For card arrays, write a 4-byte count, then each 32-byte card hash in order.
- If a signature is not present, write a length of 0 and no signature bytes.

## Binary Serialization Format: BCBlock

| Offset | Field                  | Size (bytes)      | Description                                             |
|--------|------------------------|-------------------|---------------------------------------------------------|
| 0      | version                | 1                 | Format version (e.g. 0x01)                              |
| 1      | previousHash           | 32                | Previous block hash                                     |
| 33     | timestamp              | 8                 | DateTime.ToBinary()                                     |
| 41     | transactionCount       | 4                 | Number of transactions (int, little-endian)             |
| 45     | transactions[]         | variable          | For each transaction:                                   |
|        | - txLength             | 4                 | Length of serialized transaction (int, little-endian)    |
|        | - txBytes              | N (variable)      | Serialized transaction bytes                            |

- Transactions are serialized using the transaction format tables above.

#### Example Offsets
- Offsets after variable-length fields must be computed at runtime:
    - recipientKeyLen offset = 6 + authorityKeyLen
    - recipientPublicKey offset = 10 + authorityKeyLen
    - card offset = 10 + authorityKeyLen + recipientKeyLen
    - ... etc.

### Rationale (updated)
- **Versioning:** The first byte is always the format version. This allows you to evolve the format in the future without breaking compatibility.
- **Discriminator:** The second byte identifies the transaction type, enabling polymorphic deserialization.
- **Length-prefixed fields:** Public keys and signatures may vary in length depending on encoding or algorithm. Prefixing with a 4-byte length allows for flexibility and future-proofing.
- **Explicit field order:** All fields are written in a fixed order for deterministic serialization and easy debugging.
- **Signature handling:** Allows for unsigned and signed transactions, supporting multi-step signing flows.

### Parsing Instructions
- When reading, always read the length prefix first for each variable-length field, then read the specified number of bytes.
- Update offsets as you go based on the lengths read.
- For fixed-length fields (like card hash, timestamp), read as usual.

### Evolving the Format
- When adding new fields, increment the version byte and update the deserializer to handle both old and new versions.
- Document changes in this table and in your tests.
- Use test-driven development: for each format change, add a test that asserts the exact byte layout for known input.

### Rationale
- **Versioning:** The first byte is always the format version. This allows you to evolve the format in the future without breaking compatibility.
- **Discriminator:** The second byte identifies the transaction type, enabling polymorphic deserialization.
- **Explicit field order:** All fields are written in a fixed order for deterministic serialization and easy debugging.
- **Signature handling:** Allows for unsigned and signed transactions, supporting multi-step signing flows.

### Evolving the Format
- When adding new fields, increment the version byte and update the deserializer to handle both old and new versions.
- Document changes in this table and in your tests.
- Use test-driven development: for each format change, add a test that asserts the exact byte layout for known input.

### Example Format Descriptor (C#)
```csharp
var format = new[]
{
    ("version", 1, (byte)0x01),
    ("discriminator", 1, (byte)0x01),
    ("authoritypublickey", 32, tx.AuthorityPublicKey),
    ("recipientpublickey", 32, tx.RecipientPublicKey),
    ("card", 32, tx.Card),
    ("timestamp", 8, BitConverter.GetBytes(tx.Timestamp.ToBinary())),
    ("signaturePresent", 1, (byte)(tx.AuthoritySignature != null ? 1 : 0)),
    // If present:
    // ("signatureLength", 4, BitConverter.GetBytes(tx.AuthoritySignature?.Length ?? 0)),
    // ("authoritySignature", tx.AuthoritySignature?.Length ?? 0, tx.AuthoritySignature ?? Array.Empty<byte>()),
};
```

### Test-Driven Verification
- Write tests that assert the exact byte layout for a given transaction.
- Use the format descriptor to check field order, offsets, and values.
- Example:

```csharp
int offset = 0;
foreach (var (name, length, expected) in format)
{
    var slice = bytes.Skip(offset).Take(length).ToArray();
    Assert.Equal(expected, slice);
    offset += length;
}
Assert.Equal(bytes.Length, offset); // No extra bytes
```

---

This approach ensures your binary format is robust, evolvable, and self-documenting. Update this document and your tests whenever the format changes.