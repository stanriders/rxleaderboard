import type { Metadata } from "next";

import { headers } from "next/headers";

import { FAQ } from "./faq";

import { ApiBase } from "@/api/address";
import { AllowedModsResponse } from "@/api/types";

export const metadata: Metadata = {
  title: "FAQ",
};

export default async function FaqPage() {
  const modsRequest = await fetch(`${ApiBase}/allowed-mods`, {
    headers: Object.fromEntries(headers()),
  }).catch((error) => console.log(`Allowed mods fetch failed, ${error}`));

  let modsResponse: AllowedModsResponse | undefined;

  if (modsRequest) {
    modsResponse = await modsRequest.json();
  }

  const ppVersionRequest = await fetch(`${ApiBase}/pp-version`, {
    headers: Object.fromEntries(headers()),
  }).catch((error) => console.log(`Pp version fetch failed, ${error}`));

  let ppVersionResponse: string | undefined;

  if (ppVersionRequest) {
    ppVersionResponse = await ppVersionRequest.json();
  }

  return <FAQ modsResponse={modsResponse} ppVersionResponse={ppVersionResponse}/>;
}
