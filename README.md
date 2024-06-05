# ElasticSearch_PubScreen
Indexer for Pubscreen search, leverages ElasticSearch to index existing publications, utilizing the PubScreen Database.

# How Does it Work?
It creates an indexer with a custom analyzer, stop analyzer, and keyword. The custom analyzer uses EdgeNGram Token and the following filters: lowercase, stop, classic, word_delimiter. Specifically, the Author, Keywords, and Title fields exclusively utilize the custom_analyzer for optimal precision, while Abstract field uses the stop analyzer, and DOI uses the Keyword analzyer.
