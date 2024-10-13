"use client"
import { Accordion, AccordionItem } from "@nextui-org/accordion";
import { Link } from "@nextui-org/link";
import { Card, CardBody } from "@nextui-org/react";

// this stupid thing exists because of the nextui bug that makes accordions not work in ssr
export default function FAQ() {
  return (
  <>
    <Card>
      <CardBody>
        <Accordion>  
          <AccordionItem key="1" aria-label="Slow updates" title="Why does it update so slowly? How does it even update considering there&apos;s no way to get player&apos;s RX scores from API?">
            <span className="text-sm">Website goes through <span className="italics">every</span> ranked beatmap one by one and asks osu! website to return RX scores. It can&apos;t go faster than about one query per second and with about 200 <span className="italics">thousands</span> ranked maps it takes roughly half a month to go through all of them.</span>
          </AccordionItem>
          <AccordionItem key="2" aria-label="Missing scores" title="Why not all my scores are visible on the website?">
            <span className="text-sm">Website uses <Link isExternal href="https://data.ppy.sh">all ranked beatmaps dump</Link> made by peppy which usually update once a month. You can also add scores manually <Link href="/add-score">here</Link> if you don&apos;t want to wait for automated processing.</span>
          </AccordionItem>
          <AccordionItem key="3" aria-label="Mods" title="Why only HD DT and HR scores visible?">
            <span className="text-sm">osu! API only allows to query one mod combination per query (i.e. HDRX) so with every added mod the time to go through all beatmaps grows exponantially. Currently with just these mods it&apos;s 7 times slower than if it was just RX and nothing else.</span>
          </AccordionItem>
        </Accordion>
        </CardBody>
      </Card>
  </>
  );
}
