# BlockChainCards TODO

## ✅ Completed
- [x] Wallet creation, loading, and saving
- [x] CLI parser and command structure
- [x] Mint transaction signing and authority validation
- [x] Trade (atomic swap) command and two-step signature flow
- [x] Blockchain persistence to JSON file
- [x] Show-blockchain CLI command
- [x] Fast SQLite-backed card ownership store

---

## 🟡 Card Ownership Store Integration (In Progress)
- [x] Route all ownership checks and updates through `ICardOwnershipStore`
- [ ] Add error handling/logging for ingest failures
- [ ] Ensure checkpointing logic is robust (no missed/duplicate blocks)

---

## 🟠 Blockchain Storage Upgrade (Flat File + SQLite Index)
- [x] **Switch to storing blocks in batch flat files with binary serialization**
    - [x] Design a binary serialization format for blocks (and transactions if needed)
    - [x] Implement serialization/deserialization methods for blocks
- [x] **Batch blocks into files** (e.g., `blocks_0_999.dat`)
    - [x] Write new blocks as byte arrays appended to the current file
    - [x] Rotate to a new file every N blocks (e.g., 1000)
- [x] **SQLite index for fast lookup**
    - [x] Create a SQLite table with: block hash, file name, offset, length
    - [x] On block write, insert index entry
    - [ ] On block lookup, query index and read the correct file/offset/length
- [x] **Update CLI and core logic to use new storage**
    - [x] Refactor block read/write code to use the new flat file + index system
- [ ] (Optional) Benchmark performance and tune batch size or index structure

**Notes (as of 2025-06-09):**
- Major refactor completed: All block file reading/writing and index logic now use the new BlockFileReader, BlockFileStreamHandler, and BlockFileIndex abstractions. All direct file/offset logic has been encapsulated or removed.
- Iteration, lookup, and append logic all use the new system.
- Remaining: performance benchmarking/tuning if desired.
- All new code avoids tuples and uses explicit utility classes/records for clarity and maintainability.

---

## 🟢 Proof of Stake (PoS) & Multi-Node Test Network
- [ ] **Implement coins/currency**
    - [ ] Add a coin/balance system to the blockchain
    - [ ] Support minting, transferring, and tracking coin balances
    - [ ] Add coin-balance queries to CLI
- [ ] **Design and implement staking logic**
    - [ ] Allow users to "stake" coins (lock coins to become block proposers)
    - [ ] Track staked balances per user
- [ ] **Block proposer selection (PoS round)**
    - [ ] Implement weighted random or round-robin proposer selection based on staked coins
    - [ ] Add proposer signature to block structure
    - [ ] Ensure only eligible stakers can propose blocks
- [ ] **Block validation**
    - [ ] Validate proposer eligibility and signature
    - [ ] Validate all included transactions (signatures, double-spend, etc.)
    - [ ] Update block confirmation logic for PoS
- [ ] **Networking (local multi-node testnet)**
    - [ ] Add CLI/config support for running multiple nodes with separate data directories/ports
    - [ ] Implement basic peer discovery or static peer list
    - [ ] Implement P2P message passing for blocks and transactions (e.g., TCP/UDP/HTTP on localhost)
- [ ] **Testing and simulation**
    - [ ] Run multiple local nodes to test PoS selection and block propagation
    - [ ] Add CLI commands/logs to inspect node state, stake, and block proposals
    - [ ] Simulate network partitions, node failures, and stake changes
- [ ] **(Optional) Slashing/penalties**
    - [ ] Design and implement penalties for misbehavior (e.g., double-signing, invalid blocks)

---

## 🛠️ CLI, Usability, and Enhancements
- [ ] Add card definition registry (card IDs → metadata)
- [ ] Improve CLI error messages and help output
- [ ] Add logging for actions and errors
- [ ] (Optional) Separate CLI app for issuer actions

---

## 🧪 General & Integration
- [ ] Add more integration tests for all critical flows
- [ ] Review for any missing integration between new modules (ownership, storage, validation)
- [x] Store issuer’s public key in a well-known location
- [x] Reject mint transactions not signed by issuer

---

**Tip:**
Focus next on finishing card ownership store integration in `FileBlockChainReader`, especially validating during bulk ingest and robust error handling. Then, plan the SQLite blockchain storage migration and dive into mining, nonce, and transaction pool for a full blockchain experience!