"use client";
import { Accordion, AccordionItem } from "@nextui-org/accordion";
import { Link } from "@nextui-org/link";
import { Card, CardBody } from "@nextui-org/react";
import { FC } from "react";

import { AllowedModsResponse } from "@/api/types";

type Props = {
  modsResponse: AllowedModsResponse | undefined;
  ppVersionResponse: string | undefined;
};

// this stupid thing exists because of the nextui bug that makes accordions not work in ssr
export const FAQ: FC<Props> = (props) => {
  return (
    <>
      <Card>
        <CardBody>
          <Accordion>
            <AccordionItem
              key="1"
              aria-label="Adding scores"
              title="How can I submit a score?"
            >
              <span className="text-sm">
                All RX scores should submit automatically, with a maximum delay
                of ~2 minutes.
              </span>
            </AccordionItem>
            <AccordionItem
              key="2"
              aria-label="Adding scores"
              title="How did my score not submit?"
            >
              <span className="text-sm">
                Not every mod and/or mod setting combination is allowed.
                <br />
                {props.modsResponse ? (
                  <>
                    Allowed mods:{" "}
                    <span className="text-default-500">
                      {props.modsResponse.mods?.join(", ")}
                    </span>
                    <br />
                    Allowed mod settings:{" "}
                    <span className="text-default-500">
                      {props.modsResponse.modSettings?.join(", ")}
                    </span>
                  </>
                ) : (
                  <></>
                )}
              </span>
            </AccordionItem>
            <AccordionItem
              key="3"
              aria-label="PP"
              title="Where are those pp values coming from?"
            >
              <span className="text-sm">
                Website uses{" "}
                <Link
                  isExternal
                  href="https://github.com/ppy/osu/blob/47aa2c2bfc57d1cba893d2a78a538cf739ae8329/osu.Game.Rulesets.Osu/Difficulty/OsuDifficultyCalculator.cs#L60"
                  size="sm"
                >
                  official
                </Link>{" "}
                <Link
                  isExternal
                  href="https://github.com/ppy/osu/blob/47aa2c2bfc57d1cba893d2a78a538cf739ae8329/osu.Game.Rulesets.Osu/Difficulty/OsuPerformanceCalculator.cs#L99"
                  size="sm"
                >
                  pp system
                </Link>
                .{" "}
                {props.ppVersionResponse ? (
                  <>Current version is {props.ppVersionResponse}.</>
                ) : (
                  <></>
                )}
              </span>
            </AccordionItem>
            <AccordionItem
              key="4"
              aria-label="Other questions"
              title="I have some other question!"
            >
              <span className="text-sm">
                Join the{" "}
                <Link isExternal href="https://discord.gg/p5zqFpBUc2" size="sm">
                  Discord server
                </Link>{" "}
                and ask it in the #questions channel
              </span>
            </AccordionItem>
          </Accordion>
        </CardBody>
      </Card>
    </>
  );
};
