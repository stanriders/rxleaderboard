import type { Metadata } from "next";
import FAQ from "./faq";

export const metadata: Metadata = {
  title: 'FAQ',
};

export default function FaqPage() {
  return <FAQ />;
}
