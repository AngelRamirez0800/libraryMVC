# Literature seed provenance and image attributions

The sample catalog contains three historical authors and English-language editions. ISBNs identify the listed editions; they do not identify an abstract work. Seed summaries are brief original paraphrases. Historical images are representative of an earlier edition or publication context, not necessarily the cover of the modern ISBN edition.

## Author portraits

| Asset | Depiction and credit | License/source |
|---|---|---|
| `franz-kafka.jpg` | Franz Kafka, likely 1923; photographer unidentified. | Public domain, subject to the source page's jurisdiction explanation: [Commons file](https://commons.wikimedia.org/wiki/File:Franz_Kafka,_1923.jpg). |
| `arthur-conan-doyle.jpg` | Arthur Conan Doyle, Bain News Service, publisher; Library of Congress record 2014692316. | Public domain: [Commons file](https://commons.wikimedia.org/wiki/File:1913_Arthur_Conan_Doyle_(cropped).jpg), [Library of Congress record](https://lccn.loc.gov/2014692316). |
| `fyodor-dostoevsky.jpg` | Portrait of Fyodor Dostoevsky by Vasily Perov (1872). | Public domain artwork: [Commons file](https://commons.wikimedia.org/wiki/File:Vasily_Perov_-_%D0%9F%D0%BE%D1%80%D1%82%D1%80%D0%B5%D1%82_%D0%A4.%D0%9C.%D0%94%D0%BE%D1%81%D1%82%D0%BE%D0%B5%D0%B2%D1%81%D0%BA%D0%BE%D0%B3%D0%BE_-_Google_Art_Project.jpg). |

Portraits are static files under `wwwroot/images/authors`. The current `Author` model has no portrait property, so the seed does not pretend these paths are persisted database relationships.

## Book images

All downloaded cover/page scans are JPGs under `wwwroot/images/books/seed-assets`. The seed copies each selected file into a stable path under that book's generated ID and adds a `BookImage` row with the measured file size.

| Seed asset | Work/context depicted | Credit and license/source |
|---|---|---|
| `kafka-metamorphosis.jpg` | 1916 historical edition of *Die Verwandlung*. | H.-P. Haack, CC BY 3.0; [Commons file](https://commons.wikimedia.org/wiki/File:Franz_Kafka_Die_Verwandlung_1916_Orig.-Pappband.jpg). |
| `kafka-trial.jpg` | 1925 historical edition of *Der Prozess*. | H.-P. Haack, CC BY 3.0; [Commons file](https://commons.wikimedia.org/wiki/File:Kafka_Der_Prozess_1925.jpg). |
| `kafka-castle.jpg` | Page scan from the 1926 German edition of *Das Schloß*; it is not verified as the cover. | Scan from original book, public domain/PD-scan; [Commons file](https://commons.wikimedia.org/wiki/File:De_Kafka_Schlo%C3%9F_001.jpg). |
| `kafka-amerika.jpg` | 1927 K. Wolff edition of *Amerika*. | H.-P. Haack, CC BY 3.0; [Commons file](https://commons.wikimedia.org/wiki/File:Kafka_Amerika_1927.jpg). |
| `doyle-study-scarlet.jpg` | Cover of Beeton's Christmas Annual (1887), which contains *A Study in Scarlet*; not a standalone novel cover. | David Henry Friston; public domain; [Commons file](https://commons.wikimedia.org/wiki/File:A_Study_in_Scarlet_from_Beeton%27s_Christmas_Annual_1887.jpg). |
| `doyle-adventures.jpg` | Historical first-edition cover of *The Adventures of Sherlock Holmes*. | Public domain in the source's stated jurisdiction; check outside the U.S.; [Commons file](https://commons.wikimedia.org/wiki/File:Adventures_of_sherlock_holmes.jpg). |
| `doyle-hound-baskervilles.jpg` | 1902 historical edition of *The Hound of the Baskervilles*. | Alfred Garth Jones; public-domain scan; [Commons file](https://commons.wikimedia.org/wiki/File:Cover_(Hound_of_Baskervilles,_1902).jpg). |
| `doyle-valley-fear.jpg` | Dust jacket for *The Valley of Fear* (1915). | Arthur I. Keller; public domain; [Commons file](https://commons.wikimedia.org/wiki/File:Valley_of_fear.jpg). |
| `doyle-lost-world.jpg` | Historical 1912 *The Lost World* cover/page scan. | Public domain per Commons; [Commons file](https://commons.wikimedia.org/wiki/File:Cover_(The_Lost_World,_1912).jpg). |
| `dostoevsky-crime-punishment.jpg` | 1912 collected-edition scan of *Crime and Punishment*. | Public domain (PD-old-70 per Commons); [Commons file](https://commons.wikimedia.org/wiki/File:Dostoevsky_-_Crime_and_Punishment,_Collected_Edition,_1912.djvu). |
| `dostoevsky-brothers-karamazov.jpg` | 1922 collected-edition scan of *The Brothers Karamazov*. | Public domain (PD-old-75 per Commons); [Commons file](https://commons.wikimedia.org/wiki/File:Dostoyevsky_-_The_Brothers_Karamazov,_Collected_Edition,_1922.djvu). |
| `dostoevsky-idiot.jpg` | 1916 collected-edition scan of *The Idiot*. | Public domain (PD-old-70 per Commons); [Commons file](https://commons.wikimedia.org/wiki/File:Dostoevsky_-_The_Idiot,_Collected_Edition,_1916.djvu). |
| `dostoevsky-demons.jpg` | 1913 collected edition published as *The Possessed*, an alternate title for *Demons*. | Public domain (PD-old-70 per Commons); [Commons file](https://commons.wikimedia.org/wiki/File:Dostoevsky_-_The_Possessed,_Collected_Edition,_1913.djvu). |
| `dostoevsky-gambler.jpg` | 1914 collected-edition scan of *The Gambler and Other Stories*. | Public domain (PD-old-70 per Commons); [Commons file](https://commons.wikimedia.org/wiki/File:Dostoevsky_-_The_Gambler_and_Other_Stories,_Collected_Edition,_1914.djvu). |
| `dostoevsky-house-dead.jpg` | 1915 collected-edition scan of *The House of the Dead*. | Public domain (PD-old-70 per Commons); [Commons file](https://commons.wikimedia.org/wiki/File:Dostoyevsky_-_The_House_of_the_Dead,_Collected_Edition,_1915.djvu). |

The Commons source pages are the authoritative location for the full license and provenance details. Preserve credits and link to CC BY 3.0 when redistributing those files. The local JPEG assets were re-encoded as baseline RGB JPEGs for consistent PDF/XLSX embedding; they were not cropped or retouched. The source remains the Commons file linked above.

## Titles without a book image

No BookImage is created for the following records because a suitable image could not be downloaded or verified as specific to the work:

- Kafka: *A Country Doctor*, *Letters to Milena*, and *Investigations of a dog, and other creatures*. The latter has a historical collection-page candidate, but it does not depict the specific story and was not downloaded.
- Doyle: *The Sign of the Four* and *The Return of Sherlock Holmes*.
- Dostoevsky: *Notes from Underground and the Double*. The available historical image depicts only *Notes from Underground*, not the combined ISBN volume.

Images of historical editions are not asserted to be covers for the later ISBN editions. Public-domain status can vary by jurisdiction, and the stated CC BY license for cover photographs may not resolve rights in every underlying design element. Review local law before redistributing assets commercially.

## Bibliographic edition sources

Edition metadata was checked against these Open Library records:

- **Franz Kafka:** [The Metamorphosis collection, ISBN 9780805208498](https://openlibrary.org/books/OL20276222M/The_metamorphosis_The_penal_colony_and_other_stories); [The Trial, 9780805209990](https://openlibrary.org/books/OL23241242M/The_trial); [The Castle, 9780805211061](https://openlibrary.org/books/OL7933781M/The_Castle); [Amerika, 9780805210644](https://openlibrary.org/books/OL608316M/Amerika); [A Country Doctor, 9788090217140](https://openlibrary.org/books/OL9069073M/A_Country_Doctor); [Letters to Milena, 9781473555921](https://openlibrary.org/books/OL36228766M/Letters_to_Milena); [Investigations, 9780811226899](https://openlibrary.org/books/OL27236721M/Investigations_of_a_dog_and_other_creatures).
- **Arthur Conan Doyle:** [A Study in Scarlet, ISBN 9780192837653](https://openlibrary.org/books/OL7384270M); [The Sign of the Four, 9780141395494](https://openlibrary.org/books/OL38804365M); [The Adventures of Sherlock Holmes, 9780008182229](https://openlibrary.org/books/OL34629687M); [The Hound of the Baskervilles, 9780007513093](https://openlibrary.org/books/OL33452485M); [The Return of Sherlock Holmes, 9781853260582](https://openlibrary.org/books/OL8978436M); [The Valley of Fear, 9780486844596](https://openlibrary.org/books/OL38392902M); [The Lost World, 9781306333993](https://openlibrary.org/books/OL38281058M).
- **Fyodor Dostoevsky:** [Crime and Punishment, ISBN 9780141192802](https://openlibrary.org/books/OL28512463M); [The Brothers Karamazov, 9780374528379](https://openlibrary.org/books/OL30521039M); [The Idiot, 9780375702242](https://openlibrary.org/books/OL7426192M); [Demons, 9780679734512](https://openlibrary.org/books/OL26442471M); [Notes from Underground and the Double, 9780140455120](https://openlibrary.org/books/OL25975321M); [The Gambler and Other Stories, 9780140455090](https://openlibrary.org/books/OL26081769M); [The House of the Dead, 9780486434094](https://openlibrary.org/books/OL3693603M).
