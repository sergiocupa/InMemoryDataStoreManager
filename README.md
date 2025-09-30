In-memory data storage for general-purpose use.

I tested two indexers: BPlusTree and SkipList. I found that SkipList performed slightly better, so I decided to use it. Moreover, with SkipList, I don’t need to worry about index recycling.

I implemented basic integration of object indexers with expression tree queries, enabling the use of standard LINQ queries.
I also developed an implementation based on Entity Framework, to make future refactorings or integrations easier.
In addition, a concurrency control mechanism was implemented, allowing usage with multiple threads.

The project is still under development. So far, I have only performed basic tests, and I will continue reporting new features as they are implemented.
