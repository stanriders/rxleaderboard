import type { Metadata } from "next";
import { FAQ } from "./faq";
import { ApiBase } from "@/api/address";
import { headers } from "next/headers";
import { AllowedModsResponse } from "@/api/types";

export const metadata: Metadata = {
  title: 'FAQ',
};

export default async function FaqPage() {
  const request = await fetch(`${ApiBase}/allowed-mods`, { headers: Object.fromEntries(headers()) })
  .catch(error=> console.log(`Allowed mods fetch failed, ${error}`));

  let response : AllowedModsResponse | undefined;
  if (request) {
    response = await request.json();
  }

  return <FAQ response={response} />;
}
