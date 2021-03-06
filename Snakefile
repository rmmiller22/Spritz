# SPECIES = config["species"]
# GENOME_VERSION = config["genome"]
# ENSEMBL_VERSION = config["release"]
# ENSEMBL_VERSION_SNPEFF = config["snpeff"]
# GENEMODEL_VERSION = GENOME_VERSION + "." + ENSEMBL_VERSION
# GENEMODEL_VERSION_SNPEFF = GENOME_VERSION + "." + ENSEMBL_VERSION_SNPEFF
# GENOME_FA = f"data/ensembl/{SPECIES}.{GENOME_VERSION}.dna.primary_assembly.fa"
# ENSEMBL_GFF = f"data/ensembl/{SPECIES}.{GENEMODEL_VERSION}.gff3"
# TEST_GENOME_FA = f"data/ensembl/202122.fa"
# TEST_ENSEMBL_GFF = f"data/ensembl/202122.gff3"
# FA=GENOME_FA # for analysis; can also be TEST_GENOME_FA
# GFF3=ENSEMBL_GFF # for analysis; can also be TEST_ENSEMBL_GFF
# REFSTAR_PREFIX = f"data/ensembl/{SPECIES}.{GENEMODEL_VERSION}RsemStar/RsemStarReference"
# REFSTAR_FOLDER = f"data/ensembl/{SPECIES}.{GENEMODEL_VERSION}RsemStar/"
# REF_PREFIX = f"data/ensembl/{SPECIES}.{GENEMODEL_VERSION}Rsem/RsemReference"
# REF_FOLDER = f"data/ensembl/{SPECIES}.{GENEMODEL_VERSION}Rsem/"
# REF = SPECIES + "." + GENOME_VERSION

configfile: "config.yaml"

def check_dir():
    docheck = 'analysisDirectory' in config and config["analysisDirectory"] is not None and len(config["analysisDirectory"]) > 0
    return docheck

def output(wildcards):
     if check_dir():
         return expand(["{dir}/combined.spritz.snpeff.protein.withmods.xml.gz",
         "{dir}/combined.spritz.isoformvariants.protein.withmods.xml.gz",
         "{dir}/combined.spritz.isoform.protein.withmods.xml.gz",
         "{dir}/dummy.txt"],
         dir=config["analysisDirectory"])
     return expand(["output/combined.spritz.snpeff.protein.withmods.xml.gz",
     "output/combined.spritz.isoformvariants.protein.withmods.xml.gz",
     "output/combined.spritz.isoform.protein.withmods.xml.gz",
     "output/dummy.txt"])

rule all:
    input: output

rule clean:
    shell:
        "rm -rf data/ ChromosomeMappings/ SnpEff/ tmp/ fast.tmp/ && "
        "cd GtfSharp && dotnet clean && cd .. && "
        "cd TransferUniProtModifications && dotnet clean && cd .."

include: "rules/downloads.smk"
include: "rules/align.smk"
include: "rules/variants.smk"
include: "rules/isoforms.smk"
include: "rules/proteogenomics.smk"
include: "rules/qc.smk"
include: "rules/quant.smk"
include: "rules/fusion.smk"
