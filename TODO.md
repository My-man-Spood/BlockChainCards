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

ere’s a concise analysis of what’s left to integrate from 
BlockFileReader
 and BlockFileIndex into 
FileBlockChainReader
:

What’s Already Integrated
Bulk catch-up/indexing: You’re using blockFileReader.EnumerateBlocksMetaData and blockFileIndex for efficient catch-up (
CatchupBlockIndex
).
Block appending: Presumably, new blocks are appended using BlockFileReader.AppendBlock and indexed.
Direct block file/offset logic: Most legacy logic appears refactored out.
What’s Likely Still Left To Integrate or Double-Check
1. Block Lookup by Hash/Height
Should use:
blockFileIndex.LookupByHash or LookupByHeight to get file + offset.
Then blockFileReader.ReadBlockDirect to read the block.
Check: Any methods like GetBlockByHash, GetBlockByHeight, or similar must use this pattern.
2. Block Append Logic
Should use:
blockFileReader.AppendBlock to write a new block.
Immediately update blockFileIndex with the returned metadata.
Check: All places that add blocks to storage must use this, not legacy append/file logic.
3. Chain Iteration/Enumeration
Should use:
For sequential access, use blockFileReader.EnumerateBlocksMetaData.
Check: No direct file/offset iteration remains; all iteration should use the new abstractions.
4. Index Initialization/Rebuild
Should ensure:
On startup, if the index is missing or out of sync, do a full scan and rebuild.
Check: There’s a robust check at startup for index presence and consistency, not just during catch-up.
5. Remove Old Logic
Should ensure:
No code remains that directly opens block files, seeks, or parses blocks except through 
BlockFileReader
/BlockFileIndex.
Check: Remove or refactor any legacy file/offset/block parsing code.
6. Testing/Validation
Should ensure:
All block lookups, appends, and enumerations work via the new system.
Index can be rebuilt from block files if needed.
How to Audit
Search for:
Any direct file access, file/offset logic, or old-style block parsing in 
FileBlockChainReader
.
Any places where blocks are looked up, appended, or iterated—ensure they use the new APIs.
Summary Table
Integration Point	Should Use	Check/To-Do
Block lookup (hash/height)	Index + Reader	Update all lookup methods
Block append	Reader.AppendBlock + Index	Update all append logic
Chain iteration	Reader.EnumerateBlocksMetaData	Update all iteration logic
Index rebuild/init	Scan via Reader, bulk ingest to Index	Add robust check at startup
Remove old logic	Only use Reader/Index abstractions	Audit/remove legacy file/offset code
Testing	End-to-end via new abstractions	Validate all block operations
If you want, I can help you:

Audit your 
FileBlockChainReader
 for any remaining legacy logic.
Draft/modernize any lookup/append/iteration methods.
Suggest a robust index rebuild/init routine.
Just let me know which area you want to focus on next!